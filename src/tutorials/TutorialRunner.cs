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

public sealed class TutorialRunner : IDisposable
{
	private readonly TutorialProgress _progress;
	private readonly ITutorialDialog _dialog;
	private readonly WorldLinkNavigator _worldLinks;

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
		_dialog.WorldLinkClicked += OnWorldLinkClicked;
	}

	public event Action<TutorialFlow>? Started;

	public event Action<TutorialFlow>? Completed;

	public TutorialFlow? ActiveFlow { get; private set; }

	public TutorialStartResult Start(TutorialFlow flow)
	{
		ArgumentNullException.ThrowIfNull(flow);
		ArgumentException.ThrowIfNullOrEmpty(flow.Id);
		ArgumentException.ThrowIfNullOrEmpty(flow.WorldObjectId);

		if (_progress.IsCompleted(flow.Id))
			return new TutorialStartResult.AlreadyCompleted();

		if (ActiveFlow is { } active)
			return new TutorialStartResult.Busy(active.Id);

		var navigation = _worldLinks.Follow(flow.WorldObjectId);
		if (navigation is WorldLinkNavigationResult.FocusFailed focusFailed)
			return new TutorialStartResult.FocusFailed(focusFailed.Result);
		if (navigation is WorldLinkNavigationResult.IndicatorFailed indicatorFailed)
		{
			return new TutorialStartResult.IndicatorFailed(indicatorFailed.Result);
		}

		ActiveFlow = flow;
		_dialog.Open(flow.Dialog);
		Started?.Invoke(flow);
		return new TutorialStartResult.Started();
	}

	private void OnAccepted()
	{
		var flow = ActiveFlow
			?? throw new InvalidOperationException("Tutorial dialog accepted without an active flow.");

		_dialog.Close();
		_worldLinks.Clear();
		_progress.Complete(flow.Id);
		ActiveFlow = null;
		Completed?.Invoke(flow);
	}

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
		_dialog.WorldLinkClicked -= OnWorldLinkClicked;
		if (ActiveFlow is not null)
			_dialog.Close();
		ActiveFlow = null;
		_worldLinks.Dispose();
	}
}
