using GrimSpace.Core.Actions;
using GrimSpace.Core.Engine;
using GrimSpace.World.StarSystem.Contracts;
using GrimSpace.World.StarSystem.Runtime;

namespace GrimSpace.World.StarSystem.Effects;

public record DeliveryTurnedIn(string contractId);

public sealed class RecordDeliveryTurnedInEffect(string contractId) : IEffect<StarMap, ActorRuntime>
{
	private ContractState? _previous;

	public IReadOnlyList<IRecord> Apply(StarMap world, ActorRuntime runtime, string actorId)
	{
		if (!world.ContractRegistry.TryGetState(contractId, out var state)
			|| state is not DeliveryContractState delivery)
			return [];

		_previous = state;
		world.ContractRegistry.ReplaceState(delivery.MarkLegCompleted(0));
		return [new Record<DeliveryTurnedIn>(new DeliveryTurnedIn(contractId))];
	}

	public void Undo(StarMap world, ActorRuntime runtime, string actorId)
	{
		if (_previous is not null)
			world.ContractRegistry.ReplaceState(_previous);
		_previous = null;
	}
}
