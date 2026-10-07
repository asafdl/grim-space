using GrimSpace.Core.Actions;
using GrimSpace.Core.Engine;
using GrimSpace.World.StarSystem.Actions;
using GrimSpace.World.StarSystem.Contracts;
using GrimSpace.World.StarSystem.Contracts.Objectives;
using GrimSpace.World.StarSystem.Ids;
using GrimSpace.World.StarSystem.Runtime;

namespace GrimSpace.World.StarSystem.Effects;

public record DeliveryLegCompleted(string ContractId, int LegIndex);
public record DeliveryRouteCompleted(string ContractId);

public class AdvanceDeliveryLegEffect(string contractId, int legIndex) : IEffect<StarMap, ActorRuntime>
{
	private ContractState? _previous;
	private ContractState? _applied;
	private FailDeliveryDeadlineAction? _scheduledDeadlineAction;
	private int _scheduledDeadlineTick;

	public IReadOnlyList<IRecord> Apply(StarMap world, ActorRuntime runtime, string actorId)
	{
		if (!world.ContractRegistry.TryGetState(contractId, out var state)
			|| state is not DeliveryContractState delivery)
			return [];

		if (delivery.Progress.CurrentLegIndex != legIndex
			|| legIndex < 0
			|| legIndex >= delivery.Progress.CompletedLegs.Count
			|| delivery.Progress.CompletedLegs[legIndex])
			return [];

		_previous = state;
		var next = delivery.MarkLegCompleted(legIndex);
		if (next.IsObjectiveMet())
		{
			next = next.WithDeadlineTick(null);
		}
		else if (world.ContractRegistry.TryGet(contractId, out var contract)
			&& !contract.IsStoryObjective
			&& contract.Objective is DeliveryObjective objective)
		{
			var origin = DeliveryContractState.CoordinateOf(
				world,
				objective.Route.Legs[legIndex]);
			var currentTick = world.Timeline.Clock.Current;
			var deadlineTick = DeliveryContractState.DeadlineTickForLeg(
				world,
				objective,
				next.Progress.CurrentLegIndex,
				origin,
				currentTick);
			next = next.WithDeadlineTick(deadlineTick);
			_scheduledDeadlineAction = new FailDeliveryDeadlineAction(
				StarSystemActorIds.Contracts,
				contractId);
			_scheduledDeadlineTick = deadlineTick + 1;
			world.Timeline.Schedule(
				_scheduledDeadlineTick - currentTick,
				_scheduledDeadlineAction);
		}
		world.ContractRegistry.ReplaceState(next);
		ContractDeliveryRoleSupport.OnDeliveryLegAdvanced(world, state, next);
		_applied = next;

		var records = new List<IRecord>
		{
			new Record<DeliveryLegCompleted>(
				new DeliveryLegCompleted(contractId, legIndex)),
		};
		if (next.IsObjectiveMet())
			records.Add(new Record<DeliveryRouteCompleted>(
				new DeliveryRouteCompleted(contractId)));
		return records;
	}

	public void Undo(StarMap world, ActorRuntime runtime, string actorId)
	{
		if (_previous is not null)
		{
			if (_applied is not null)
				ContractDeliveryRoleSupport.OnDeliveryLegAdvanced(world, _applied, _previous);
			world.ContractRegistry.ReplaceState(_previous);
		}
		if (_scheduledDeadlineAction is not null)
			world.Timeline.CancelPending(_scheduledDeadlineTick, _scheduledDeadlineAction);
		_previous = null;
		_applied = null;
		_scheduledDeadlineAction = null;
		_scheduledDeadlineTick = 0;
	}
}
