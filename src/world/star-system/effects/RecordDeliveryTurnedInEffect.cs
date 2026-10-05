using GrimSpace.Core.Actions;
using GrimSpace.Core.Engine;
using GrimSpace.World.StarSystem.Contracts;
using GrimSpace.World.StarSystem.Runtime;

namespace GrimSpace.World.StarSystem.Effects;

public record DeliveryLegCompleted(string ContractId, int LegIndex);
public record DeliveryRouteCompleted(string ContractId);

public class AdvanceDeliveryLegEffect(string contractId, int legIndex) : IEffect<StarMap, ActorRuntime>
{
	private ContractState? _previous;
	private ContractState? _applied;

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
		_previous = null;
		_applied = null;
	}
}

