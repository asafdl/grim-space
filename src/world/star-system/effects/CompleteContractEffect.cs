using GrimSpace.Core.Actions;
using GrimSpace.Core.Engine;
using GrimSpace.World.StarSystem.Contracts;
using GrimSpace.World.StarSystem.Runtime;

namespace GrimSpace.World.StarSystem.Effects;

public sealed class CompleteContractEffect(string contractId)
	: IEffect<StarMap, ActorRuntime>
{
	private ContractState? _previous;

	public IReadOnlyList<IRecord> Apply(StarMap world, ActorRuntime runtime, string actorId)
	{
		if (!world.ContractRegistry.TryGetState(contractId, out _previous))
			throw new InvalidOperationException($"Contract '{contractId}' has no runtime state.");

		world.ContractRegistry.Complete(contractId);
		ContractDeliveryRoleSupport.OnContractEnded(world, contractId);
		return [];
	}

	public void Undo(StarMap world, ActorRuntime runtime, string actorId)
	{
		if (_previous is null)
			throw new InvalidOperationException($"Contract '{contractId}' completion was not applied.");

		world.ContractRegistry.Restore(_previous);
		ContractDeliveryRoleSupport.OnContractActivated(world, _previous);
		_previous = null;
	}
}
