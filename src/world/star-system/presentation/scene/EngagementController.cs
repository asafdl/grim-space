using GrimSpace.World.StarSystem.Contact;
using GrimSpace.World.StarSystem.Presentation.Ui;

namespace GrimSpace.World.StarSystem.Presentation.Scene;

public sealed class EngagementController : IDisposable
{
	private readonly EngagementHudOverlay _hud;
	private readonly DeploymentSlots _deployment;
	private readonly Action<DeploymentSlots> _resetDeployment;
	private readonly Func<IReadOnlyList<string>, PendingEngagement?> _pendingEngagement;
	private readonly Func<IReadOnlyList<string>, bool> _tryEngage;
	private readonly Func<bool> _tryFlee;
	private readonly Action<string, string> _presentSystemError;
	private readonly IDisposable _contactSubscription;

	public EngagementController(
		EngagementHudOverlay hud,
		DeploymentSlots deployment,
		Action<DeploymentSlots> resetDeployment,
		Func<IReadOnlyList<string>, PendingEngagement?> pendingEngagement,
		Func<IReadOnlyList<string>, bool> tryEngage,
		Func<bool> tryFlee,
		Func<Action, IDisposable> subscribeContact,
		Action<string, string> presentSystemError)
	{
		ArgumentNullException.ThrowIfNull(presentSystemError);
		_hud = hud;
		_deployment = deployment;
		_resetDeployment = resetDeployment;
		_pendingEngagement = pendingEngagement;
		_tryEngage = tryEngage;
		_tryFlee = tryFlee;
		_presentSystemError = presentSystemError;
		_hud.EngageRequested += OnEngageRequested;
		_hud.FleeRequested += OnFleeRequested;
		_deployment.Changed += SyncSelection;
		_contactSubscription = subscribeContact(Sync);
		Sync();
	}

	public bool IsOpen => _hud.IsOpen;

	public bool TryHandleInput(Godot.InputEvent @event) => _hud.TryHandleInput(@event);

	private void Sync()
	{
		// Reset raises Changed, which syncs the HUD.
		if (!_hud.IsOpen && _pendingEngagement(_deployment.DeployedPilotIds) is not null)
			_resetDeployment(_deployment);
		else
			SyncSelection();
	}

	private void SyncSelection()
	{
		if (_pendingEngagement(_deployment.DeployedPilotIds) is { } pending)
			_hud.Sync(pending, _deployment);
		else if (_hud.IsOpen)
			_hud.Close();
	}

	private void OnEngageRequested()
	{
		_hud.SetBusy(true);
		if (_tryEngage(_deployment.DeployedPilotIds))
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
		_deployment.Changed -= SyncSelection;
		_hud.EngageRequested -= OnEngageRequested;
		_hud.FleeRequested -= OnFleeRequested;
	}
}
