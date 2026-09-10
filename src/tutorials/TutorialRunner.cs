using GrimSpace.Education;

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
	private readonly IWorldFocus _worldFocus;
	private readonly IWorldIndicator _worldIndicator;
	private IWorldIndicatorHandle? _indicatorHandle;

	public TutorialRunner(
		TutorialProgress progress,
		ITutorialDialog dialog,
		IWorldFocus worldFocus,
		IWorldIndicator worldIndicator)
	{
		_progress = progress ?? throw new ArgumentNullException(nameof(progress));
		_dialog = dialog ?? throw new ArgumentNullException(nameof(dialog));
		_worldFocus = worldFocus ?? throw new ArgumentNullException(nameof(worldFocus));
		_worldIndicator = worldIndicator ?? throw new ArgumentNullException(nameof(worldIndicator));
		_dialog.Accepted += OnAccepted;
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

		var indicator = _worldIndicator.Show(flow.WorldObjectId);
		if (indicator is not WorldIndicatorResult.Shown shown)
			return new TutorialStartResult.IndicatorFailed(indicator);

		var focus = _worldFocus.Focus(flow.WorldObjectId);
		if (focus is not WorldFocusResult.Accepted)
		{
			shown.Handle.Dispose();
			return new TutorialStartResult.FocusFailed(focus);
		}

		_indicatorHandle = shown.Handle;
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
		ClearIndicator();
		_progress.Complete(flow.Id);
		ActiveFlow = null;
		Completed?.Invoke(flow);
	}

	private void ClearIndicator()
	{
		_indicatorHandle?.Dispose();
		_indicatorHandle = null;
	}

	public void Dispose()
	{
		_dialog.Accepted -= OnAccepted;
		if (ActiveFlow is not null)
			_dialog.Close();
		ActiveFlow = null;
		ClearIndicator();
	}
}
