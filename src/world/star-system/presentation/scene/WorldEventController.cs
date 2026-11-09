using GrimSpace.Core.Actions;
using GrimSpace.Run;
using GrimSpace.World.StarSystem.Actions;
using GrimSpace.World.StarSystem.Events;
using GrimSpace.World.StarSystem.Presentation.Ui;

namespace GrimSpace.World.StarSystem.Presentation.Scene;

public sealed class WorldEventController : IDisposable
{
	private readonly IWorldEventHud _hud;
	private readonly Func<PendingWorldEvent?> _pendingEvent;
	private readonly Func<IAction, bool> _tryEnqueue;
	private readonly Action<string, string> _presentSystemError;
	private readonly IDisposable _impactSubscription;

	public WorldEventController(
		IWorldEventHud hud,
		Func<PendingWorldEvent?> pendingEvent,
		Func<IAction, bool> tryEnqueue,
		Func<Action, IDisposable> subscribeImpact,
		Action<string, string> presentSystemError)
	{
		ArgumentNullException.ThrowIfNull(presentSystemError);
		_hud = hud;
		_pendingEvent = pendingEvent;
		_tryEnqueue = tryEnqueue;
		_presentSystemError = presentSystemError;
		_hud.DismissRequested += OnDismissRequested;
		_impactSubscription = subscribeImpact(Sync);
		Sync();
	}

	public bool IsOpen => _hud.IsOpen;

	public bool TryHandleInput(Godot.InputEvent @event) => _hud.TryHandleInput(@event);

	private void Sync()
	{
		if (_pendingEvent() is { } pending)
			_hud.Sync(pending.Presentation, pending.Reward);
		else if (_hud.IsOpen)
			_hud.Close();
	}

	private void OnDismissRequested()
	{
		if (_pendingEvent() is not { } pending)
		{
			_hud.Close();
			return;
		}

		_hud.SetBusy(true);
		if (_tryEnqueue(Acknowledge(pending)))
		{
			_hud.Close();
			return;
		}

		_hud.Close();
		_presentSystemError(
			"Could not acknowledge the event.",
			"Acknowledge action was rejected while a world event announcement was displayed.");
	}

	public void Dispose()
	{
		_impactSubscription.Dispose();
		_hud.DismissRequested -= OnDismissRequested;
	}

	internal static IDisposable Join(IDisposable first, IDisposable second) =>
		new JoinedSubscriptions(first, second);

	private static IAction Acknowledge(PendingWorldEvent pending) =>
		pending.EventId switch
		{
			EWorldEventId.AsteroidAccident =>
				new AcknowledgeAsteroidImpactAction(State.PlayerFleetUnitId, pending.Id),
			EWorldEventId.DriftSalvage =>
				new AcknowledgeDriftSalvageAction(State.PlayerFleetUnitId, pending.Id),
			_ => throw new InvalidOperationException(
				$"No acknowledge action is registered for {pending.EventId}."),
		};

	private sealed class JoinedSubscriptions(IDisposable first, IDisposable second) : IDisposable
	{
		public void Dispose()
		{
			first.Dispose();
			second.Dispose();
		}
	}
}
