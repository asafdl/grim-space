using GrimSpace.Core.Actions;
using GrimSpace.Core.Engine;
using GrimSpace.World.StarSystem.Contracts;
using GrimSpace.World.StarSystem.Runtime;

namespace GrimSpace.World.StarSystem.Effects;

public sealed class SetDeliveryTurnedInEffect(string contractId) : IEffect<StarMap, ActorRuntime>
{
	private ContractState? _previous;

	public IReadOnlyList<IRecord> Apply(StarMap world, ActorRuntime runtime, string actorId)
	{
		if (!world.ContractRegistry.TryGetState(contractId, out var state)
			|| state.Status != EContractStatus.Active)
			throw new InvalidOperationException($"Contract '{contractId}' is not active.");

		_previous = state;
		world.ContractRegistry.Restore(state with { DeliveryTurnedIn = true });
		return [];
	}

	public void Undo(StarMap world, ActorRuntime runtime, string actorId)
	{
		if (_previous is null)
			throw new InvalidOperationException($"Contract '{contractId}' turn-in was not applied.");

		world.ContractRegistry.Restore(_previous);
		_previous = null;
	}
}
