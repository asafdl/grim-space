using GrimSpace.World.StarSystem.Contact;
using GrimSpace.World.StarSystem.Presentation.Ui;

namespace GrimSpace.World.StarSystem.Presentation.Scene;

public sealed class EngagementController : IDisposable
{
	private readonly EngagementHudOverlay _hud;
	private readonly Func<PendingEngagement?> _pendingEngagement;
	private readonly Func<bool> _tryEngage;
	private readonly Func<bool> _tryFlee;
	private readonly Action<string, string> _presentSystemError;
	private readonly IDisposable _contactSubscription;

	public EngagementController(
		EngagementHudOverlay hud,
		Func<PendingEngagement?> pendingEngagement,
		Func<bool> tryEngage,
		Func<bool> tryFlee,
		Func<Action, IDisposable> subscribeContact,
		Action<string, string> presentSystemError)
	{
		ArgumentNullException.ThrowIfNull(presentSystemError);
		_hud = hud;
		_pendingEngagement = pendingEngagement;
		_tryEngage = tryEngage;
		_tryFlee = tryFlee;
		_presentSystemError = presentSystemError;
		_hud.EngageRequested += OnEngageRequested;
		_hud.FleeRequested += OnFleeRequested;
		_contactSubscription = subscribeContact(Sync);
		Sync();
	}

	public bool IsOpen => _hud.IsOpen;

	public bool TryHandleInput(Godot.InputEvent @event) => _hud.TryHandleInput(@event);

	private void Sync()
	{
		if (_pendingEngagement() is { } pending)
			_hud.Sync(pending);
		else if (_hud.IsOpen)
			_hud.Close();
	}

	private void OnEngageRequested()
	{
		_hud.SetBusy(true);
		if (_tryEngage())
		{
			_hud.Close();
			return;
		}

		_hud.Close();
		_presentSystemError(
			"Could not start engagement.",
			"Engage action was rejected while a pending player engagement was displayed.");
	}

	private void OnFleeRequested()
	{
		_hud.SetBusy(true);
		if (_tryFlee())
		{
			_hud.Close();
			return;
		}

		_hud.Close();
		_presentSystemError(
			"Could not leave engagement.",
			"Flee action was rejected while a pending player engagement was displayed.");
	}

	public void Dispose()
	{
		_contactSubscription.Dispose();
		_hud.EngageRequested -= OnEngageRequested;
		_hud.FleeRequested -= OnFleeRequested;
	}
}
