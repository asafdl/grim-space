using GrimSpace.Core.Actions;
using GrimSpace.Core.Engine;
using GrimSpace.World.StarSystem.Actions;
using GrimSpace.World.StarSystem.Contact;
using GrimSpace.World.StarSystem.Contracts;
using GrimSpace.World.StarSystem.Runtime;

namespace GrimSpace.World.StarSystem.Effects;

public sealed class EndContractEffect(
	string contractId,
	EContractStatus status,
	EDeliveryFailureReason? deliveryFailureReason = null)
	: IEffect<StarMap, ActorRuntime>
{
	private ContractState? _previous;
	private Units.TravelTarget? _previousHolderTravelTarget;
	private Units.Engagement? _previousHolderEngagement;
	private Units.Fleet? _removedInterceptor;
	private Units.FleetPursuitDirective? _previousInterceptorDirective;
	private Units.TravelTarget? _previousInterceptorTravelTarget;
	private Units.Engagement? _previousInterceptorEngagement;
	private ReturnToPatrolAction? _scheduledPatrolReturn;
	private int _scheduledPatrolReturnTick;
	private readonly List<Units.Fleet> _removedDeliveryFleets = [];

	public IReadOnlyList<IRecord> Apply(StarMap world, ActorRuntime runtime, string actorId)
	{
		if (!world.ContractRegistry.TryGetState(contractId, out _previous))
			throw new InvalidOperationException($"Contract '{contractId}' has no runtime state.");

		if (_previous.HolderUnitId is { } holderUnitId
			&& world.FleetRegistry.TryGet(holderUnitId, out var holder))
		{
			_previousHolderTravelTarget = holder.State.TravelTarget;
			_previousHolderEngagement = holder.State.CurrentEngagement;
		}
		if (_previous is DeliveryContractState deliveryState
			&& deliveryState.Progress.InterceptorFleetId is { } interceptorFleetId
			&& world.FleetRegistry.TryGet(interceptorFleetId, out var interceptor))
		{
			_removedInterceptor = interceptor;
			_previousInterceptorDirective = interceptor.State.PursuitDirective;
			_previousInterceptorTravelTarget = interceptor.State.TravelTarget;
			_previousInterceptorEngagement = interceptor.State.CurrentEngagement;
		}
		if (_previous is DeliveryContractState
			&& world.ContractRegistry.TryGet(contractId, out var deliveryContract)
			&& deliveryContract.Objective is Contracts.Objectives.DeliveryObjective deliveryObjective)
		{
			foreach (var meetingId in deliveryObjective.Route.Legs
				.OfType<Contracts.Objectives.SpaceMeetingDeliveryLeg>()
				.Select(leg => leg.MeetingId))
			{
				if (world.FleetRegistry.TryGet(meetingId, out var meetingFleet)
					&& string.Equals(
						meetingFleet.State.SourceContractId,
						contractId,
						StringComparison.Ordinal))
					_removedDeliveryFleets.Add(meetingFleet);
			}
		}

		if (deliveryFailureReason is { } failureReason)
		{
			if (status != EContractStatus.Failed || _previous is not DeliveryContractState delivery)
			{
				throw new InvalidOperationException(
					$"Delivery failure reason '{failureReason}' cannot be applied to contract '{contractId}'.");
			}

			world.ContractRegistry.ReplaceState(delivery.WithFailureReason(failureReason));
			DeliveryDiagnostics.Fail(contractId, failureReason);
		}

		switch (status)
		{
			case EContractStatus.Completed:
				world.ContractRegistry.Complete(contractId);
				break;
			case EContractStatus.Failed:
				world.ContractRegistry.Fail(contractId);
				break;
			default:
				throw new ArgumentOutOfRangeException(nameof(status), status, null);
		}

		var records = new List<IRecord>();
		if (_removedInterceptor is not null
			&& _previous.HolderUnitId is { } pursuitTargetId)
		{
			var wasActivelyPursuing =
				(string.Equals(
						_previousInterceptorTravelTarget?.TargetId,
						pursuitTargetId,
						StringComparison.Ordinal)
					&& _previousInterceptorTravelTarget?.ContactIntent
						== EContactIntent.Engagement)
				|| string.Equals(
					_previousInterceptorEngagement?.Hunting,
					pursuitTargetId,
					StringComparison.Ordinal);
			var wasAssigned = string.Equals(
					_previousInterceptorDirective?.TargetFleetId,
					pursuitTargetId,
					StringComparison.Ordinal);
			if (wasAssigned || wasActivelyPursuing)
			{
				var change = ClearPursueContactEffect.ClearPursuit(
					world,
					_removedInterceptor.State.Id);
				if (change is not null)
					records.Add(new Record<FleetPursuitChanged>(change));
			}

			if (wasActivelyPursuing && _removedInterceptor.State.PatrolRadius > 0)
			{
				_scheduledPatrolReturn = new ReturnToPatrolAction(_removedInterceptor.State.Id);
				_scheduledPatrolReturnTick = world.Timeline.Clock.Current + 1;
				world.Timeline.Schedule(1, _scheduledPatrolReturn);
			}
		}

		ContractDeliveryRoleSupport.OnContractEnded(world, contractId);
		if (_previous.HolderUnitId is { } endedHolderUnitId)
			records.Add(new Record<ContractStateChanged>(new ContractStateChanged(
				contractId,
				endedHolderUnitId,
				status)));

		return records;
	}

	public void Undo(StarMap world, ActorRuntime runtime, string actorId)
	{
		if (_previous is null)
			throw new InvalidOperationException($"Contract '{contractId}' ending was not applied.");

		if (_scheduledPatrolReturn is not null)
			world.Timeline.CancelPending(_scheduledPatrolReturnTick, _scheduledPatrolReturn);
		world.ContractRegistry.Restore(_previous);
		foreach (var fleet in _removedDeliveryFleets)
		{
			if (!world.FleetRegistry.Contains(fleet.State.Id))
				world.FleetRegistry.Add(fleet);
		}
		if (_removedInterceptor is not null
			&& !world.FleetRegistry.Contains(_removedInterceptor.State.Id))
			world.FleetRegistry.Add(_removedInterceptor);
		ContractDeliveryRoleSupport.OnContractActivated(world, _previous);
		if (_removedInterceptor is not null
			&& world.FleetRegistry.TryGet(_removedInterceptor.State.Id, out var interceptor))
		{
			interceptor.State.PursuitDirective = _previousInterceptorDirective;
			if (_previousInterceptorTravelTarget is { } interceptorTravelTarget)
				interceptor.State.TravelTarget = interceptorTravelTarget;
			interceptor.State.CurrentEngagement = _previousInterceptorEngagement;
		}
		if (_previousHolderTravelTarget is { } travelTarget
			&& _previous.HolderUnitId is { } holderUnitId
			&& world.FleetRegistry.TryGet(holderUnitId, out var holder))
		{
			holder.State.TravelTarget = travelTarget;
			holder.State.CurrentEngagement = _previousHolderEngagement;
		}

		_previous = null;
		_previousHolderTravelTarget = null;
		_previousHolderEngagement = null;
		_removedInterceptor = null;
		_previousInterceptorDirective = null;
		_previousInterceptorTravelTarget = null;
		_previousInterceptorEngagement = null;
		_scheduledPatrolReturn = null;
		_scheduledPatrolReturnTick = 0;
		_removedDeliveryFleets.Clear();
	}
}
