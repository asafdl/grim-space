using GrimSpace.World.StarSystem.Contact;
using GrimSpace.World.StarSystem.Presentation.Ui;

namespace GrimSpace.World.StarSystem.Presentation.Scene;

public sealed class WreckController : IDisposable
{
	private readonly WreckHudOverlay _hud;
	private readonly Func<PendingWreckDecision?> _pendingWreck;
	private readonly Func<bool> _tryInvestigate;
	private readonly Func<bool> _tryLeave;
	private readonly Action<string, string> _presentSystemError;
	private readonly IDisposable _reachSubscription;

	public WreckController(
		WreckHudOverlay hud,
		Func<PendingWreckDecision?> pendingWreck,
		Func<bool> tryInvestigate,
		Func<bool> tryLeave,
		Func<Action, IDisposable> subscribeReach,
		Action<string, string> presentSystemError)
	{
		ArgumentNullException.ThrowIfNull(presentSystemError);
		_hud = hud;
		_pendingWreck = pendingWreck;
		_tryInvestigate = tryInvestigate;
		_tryLeave = tryLeave;
		_presentSystemError = presentSystemError;
		_hud.InvestigateRequested += OnInvestigateRequested;
		_hud.LeaveRequested += OnLeaveRequested;
		_reachSubscription = subscribeReach(Sync);
		Sync();
	}

	public bool IsOpen => _hud.IsOpen;

	public bool TryHandleInput(Godot.InputEvent @event) => _hud.TryHandleInput(@event);

	private void Sync()
	{
		if (_pendingWreck() is { } pending)
			_hud.Sync(pending);
		else if (_hud.IsOpen)
			_hud.Close();
	}

	private void OnInvestigateRequested()
	{
		_hud.SetBusy(true);
		if (_tryInvestigate())
		{
			_hud.Close();
			return;
		}

		_hud.Close();
		_presentSystemError(
			"Could not investigate wreckage.",
			"Investigate wreckage action was rejected while a pending wreck decision was displayed.");
	}

	private void OnLeaveRequested()
	{
		_hud.SetBusy(true);
		if (_tryLeave())
		{
			_hud.Close();
			return;
		}

		var wasAmbush = _pendingWreck()?.IsAmbush == true;
		_hud.Close();
		_presentSystemError(
			wasAmbush ? "Could not flee the ambush." : "Could not leave wreckage.",
			"Leave wreckage action was rejected while a pending wreck decision was displayed.");
	}

	public void Dispose()
	{
		_reachSubscription.Dispose();
		_hud.InvestigateRequested -= OnInvestigateRequested;
		_hud.LeaveRequested -= OnLeaveRequested;
	}
}
