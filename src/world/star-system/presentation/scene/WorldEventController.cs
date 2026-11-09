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
		_hud.AcceptRequested += OnAcceptRequested;
		_hud.RejectRequested += OnRejectRequested;
		_impactSubscription = subscribeImpact(Sync);
		Sync();
	}

	public bool IsOpen => _hud.IsOpen;

	public bool TryHandleInput(Godot.InputEvent @event) => _hud.TryHandleInput(@event);

	private void Sync()
	{
		if (_pendingEvent() is { } pending)
			_hud.Sync(
				pending.Presentation,
				pending.Reward,
				pending.Recruit,
				pending.EventId == EWorldEventId.CompulsiveStake);
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

	private void OnAcceptRequested()
	{
		if (_pendingEvent() is not { } pending)
		{
			_hud.Close();
			return;
		}

		_hud.SetBusy(true);
		if (Choice(pending, accept: true) is not { } action)
		{
			_hud.Close();
			return;
		}

		if (_tryEnqueue(action))
		{
			_hud.Close();
			return;
		}

		_hud.Close();
		var (summary, detail) = pending.EventId == EWorldEventId.CompulsiveStake
			? ("Could not pay the stake.", "Pay action was rejected while a stake demand was displayed.")
			: ("Could not accept the recruit.", "Accept recruit action was rejected while a distress call was displayed.");
		_presentSystemError(summary, detail);
	}

	private void OnRejectRequested()
	{
		if (_pendingEvent() is not { } pending)
		{
			_hud.Close();
			return;
		}

		_hud.SetBusy(true);
		if (Choice(pending, accept: false) is not { } action)
		{
			_hud.Close();
			return;
		}

		if (_tryEnqueue(action))
		{
			_hud.Close();
			return;
		}

		_hud.Close();
		var (summary, detail) = pending.EventId == EWorldEventId.CompulsiveStake
			? ("Could not let the pilot walk.", "Refuse action was rejected while a stake demand was displayed.")
			: ("Could not reject the recruit.", "Reject recruit action was rejected while a distress call was displayed.");
		_presentSystemError(summary, detail);
	}

	public void Dispose()
	{
		_impactSubscription.Dispose();
		_hud.DismissRequested -= OnDismissRequested;
		_hud.AcceptRequested -= OnAcceptRequested;
		_hud.RejectRequested -= OnRejectRequested;
	}

	internal static IDisposable Join(params IDisposable[] subscriptions) =>
		new JoinedSubscriptions(subscriptions);

	private static IAction? Choice(PendingWorldEvent pending, bool accept) =>
		(pending.EventId, accept) switch
		{
			(EWorldEventId.DistressCall, true) =>
				new AcceptDistressRecruitAction(State.PlayerFleetUnitId, pending.Id),
			(EWorldEventId.DistressCall, false) =>
				new RejectDistressRecruitAction(State.PlayerFleetUnitId, pending.Id),
			(EWorldEventId.CompulsiveStake, true) =>
				new PayCompulsiveStakeAction(State.PlayerFleetUnitId, pending.Id),
			(EWorldEventId.CompulsiveStake, false) =>
				new RefuseCompulsiveStakeAction(State.PlayerFleetUnitId, pending.Id),
			_ => null,
		};

	private static IAction Acknowledge(PendingWorldEvent pending) =>
		pending.EventId switch
		{
			EWorldEventId.AsteroidAccident =>
				new AcknowledgeAsteroidImpactAction(State.PlayerFleetUnitId, pending.Id),
			EWorldEventId.DriftSalvage =>
				new AcknowledgeDriftSalvageAction(State.PlayerFleetUnitId, pending.Id),
			EWorldEventId.DistressCall =>
				new AcknowledgeDistressCallAction(State.PlayerFleetUnitId, pending.Id),
			_ => throw new InvalidOperationException(
				$"No acknowledge action is registered for {pending.EventId}."),
		};

	private sealed class JoinedSubscriptions(IDisposable[] subscriptions) : IDisposable
	{
		public void Dispose()
		{
			foreach (var subscription in subscriptions)
				subscription.Dispose();
		}
	}
}
