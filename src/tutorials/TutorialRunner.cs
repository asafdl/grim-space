using GrimSpace.Education;
using Godot;

namespace GrimSpace.Tutorials;

public abstract record TutorialStartResult
{
	public sealed record Started : TutorialStartResult;

	public sealed record AlreadyCompleted : TutorialStartResult;

	public sealed record Busy(string ActiveTutorialId) : TutorialStartResult;

	public sealed record FocusFailed(WorldFocusResult Result) : TutorialStartResult;

	public sealed record IndicatorFailed(WorldIndicatorResult Result) : TutorialStartResult;
}

public abstract record TutorialAdvanceResult
{
	public sealed record Advanced : TutorialAdvanceResult;

	public sealed record Completed : TutorialAdvanceResult;

	public sealed record NoActiveFlow : TutorialAdvanceResult;

	public sealed record FocusFailed(WorldFocusResult Result) : TutorialAdvanceResult;

	public sealed record IndicatorFailed(WorldIndicatorResult Result) : TutorialAdvanceResult;
}

public sealed class TutorialRunner : IDisposable
{
	private readonly TutorialProgress _progress;
	private readonly ITutorialDialog _dialog;
	private readonly WorldLinkNavigator _worldLinks;
	private int _activeStepIndex = -1;

	public TutorialRunner(
		TutorialProgress progress,
		ITutorialDialog dialog,
		IWorldFocus worldFocus,
		IWorldIndicator worldIndicator)
	{
		_progress = progress ?? throw new ArgumentNullException(nameof(progress));
		_dialog = dialog ?? throw new ArgumentNullException(nameof(dialog));
		_worldLinks = new WorldLinkNavigator(
			worldFocus ?? throw new ArgumentNullException(nameof(worldFocus)),
			worldIndicator ?? throw new ArgumentNullException(nameof(worldIndicator)));
		_dialog.Accepted += OnAccepted;
		_dialog.AssistanceRequested += OnAssistanceRequested;
		_dialog.WorldLinkClicked += OnWorldLinkClicked;
	}

	public event Action<TutorialFlow>? Started;

	public event Action<TutorialFlow, TutorialStep>? StepStarted;

	public event Action? AssistanceRequested;

	public event Action<TutorialFlow>? Completed;

	public TutorialFlow? ActiveFlow { get; private set; }

	public TutorialStep? ActiveStep =>
		ActiveFlow is { } flow && _activeStepIndex >= 0
			? flow.Steps[_activeStepIndex]
			: null;

	public TutorialStartResult Start(TutorialFlow flow)
	{
		ArgumentNullException.ThrowIfNull(flow);
		ArgumentException.ThrowIfNullOrEmpty(flow.Id);
		ArgumentNullException.ThrowIfNull(flow.Steps);
		if (flow.Steps.Count == 0)
			throw new ArgumentException("Tutorial flow must contain at least one step.", nameof(flow));
		foreach (var candidate in flow.Steps)
		{
			ArgumentNullException.ThrowIfNull(candidate);
			if (candidate.TargetId is not null)
				ArgumentException.ThrowIfNullOrEmpty(candidate.TargetId);
			ArgumentNullException.ThrowIfNull(candidate.Dialog);
			if (candidate.AdvanceOnAccept)
				ArgumentException.ThrowIfNullOrEmpty(candidate.Dialog.AcceptText);
			else if (candidate.Dialog.AcceptText is not null)
				throw new ArgumentException(
					"Externally advanced tutorial steps cannot show an accept button.",
					nameof(flow));
		}

		if (_progress.IsCompleted(flow.Id))
			return new TutorialStartResult.AlreadyCompleted();

		if (ActiveFlow is { } active)
			return new TutorialStartResult.Busy(active.Id);

		var step = flow.Steps[0];
		var navigation = PresentTarget(step);
		if (navigation is WorldLinkNavigationResult.FocusFailed focusFailed)
			return new TutorialStartResult.FocusFailed(focusFailed.Result);
		if (navigation is WorldLinkNavigationResult.IndicatorFailed indicatorFailed)
		{
			return new TutorialStartResult.IndicatorFailed(indicatorFailed.Result);
		}

		ActiveFlow = flow;
		_activeStepIndex = 0;
		_dialog.Open(step.Dialog);
		Started?.Invoke(flow);
		StepStarted?.Invoke(flow, step);
		return new TutorialStartResult.Started();
	}

	public TutorialAdvanceResult AdvanceActive()
	{
		if (ActiveFlow is not { } flow)
			return new TutorialAdvanceResult.NoActiveFlow();

		var nextStepIndex = _activeStepIndex + 1;
		if (nextStepIndex >= flow.Steps.Count)
		{
			CompleteFlow();
			return new TutorialAdvanceResult.Completed();
		}

		_worldLinks.ClearIndicator();
		if (ActiveStep is
			{
				TargetId: not null,
				FocusTarget: true,
				RetainFocusAfterStep: false,
			})
		{
			_worldLinks.ClearCurrentFocus();
		}

		var step = flow.Steps[nextStepIndex];
		var navigation = PresentTarget(step);
		if (navigation is WorldLinkNavigationResult.FocusFailed focusFailed)
			return new TutorialAdvanceResult.FocusFailed(focusFailed.Result);
		if (navigation is WorldLinkNavigationResult.IndicatorFailed indicatorFailed)
		{
			return new TutorialAdvanceResult.IndicatorFailed(indicatorFailed.Result);
		}

		_activeStepIndex = nextStepIndex;
		_dialog.Open(step.Dialog);
		StepStarted?.Invoke(flow, step);
		return new TutorialAdvanceResult.Advanced();
	}

	public void ShowAssistance(TutorialAssistanceContent content)
	{
		if (ActiveFlow is null)
			throw new InvalidOperationException("Cannot show assistance without an active tutorial.");
		_dialog.ShowAssistance(content);
	}

	public void ClearAssistance() => _dialog.ClearAssistance();

	private void CompleteFlow()
	{
		var flow = ActiveFlow
			?? throw new InvalidOperationException("Cannot complete a tutorial without an active flow.");

		_dialog.Close();
		_worldLinks.Clear();
		_progress.Complete(flow.Id);
		ActiveFlow = null;
		_activeStepIndex = -1;
		Completed?.Invoke(flow);
	}

	private WorldLinkNavigationResult PresentTarget(TutorialStep step)
	{
		if (step.TargetId is not null)
		{
			return _worldLinks.Follow(
				step.TargetId,
				step.FocusTarget,
				step.ShowIndicator);
		}

		_worldLinks.ClearIndicator();
		return new WorldLinkNavigationResult.Followed();
	}

	private void OnAccepted()
	{
		if (ActiveStep is not { AdvanceOnAccept: true })
			return;

		var result = AdvanceActive();
		if (result is TutorialAdvanceResult.FocusFailed or TutorialAdvanceResult.IndicatorFailed)
		{
			GD.PushWarning(
				$"Tutorial could not advance to its next step: {result.GetType().Name}.");
		}
	}

	private void OnAssistanceRequested() => AssistanceRequested?.Invoke();

	private void OnWorldLinkClicked(string objectId)
	{
		if (ActiveFlow is null)
		{
			GD.PushWarning("Tutorial link clicked without an active flow.");
			return;
		}

		var result = _worldLinks.Follow(objectId);
		if (result is not WorldLinkNavigationResult.Followed)
		{
			GD.PushWarning(
				$"Tutorial world link '{objectId}' failed: {result.GetType().Name}.");
		}
	}

	public void Dispose()
	{
		_dialog.Accepted -= OnAccepted;
		_dialog.AssistanceRequested -= OnAssistanceRequested;
		_dialog.WorldLinkClicked -= OnWorldLinkClicked;
		if (ActiveFlow is not null)
			_dialog.Close();
		ActiveFlow = null;
		_activeStepIndex = -1;
		_worldLinks.Dispose();
	}
}
