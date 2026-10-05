using GrimSpace.Core.Actions;
using GrimSpace.Core.Engine;
using GrimSpace.World.StarSystem.Contracts;
using GrimSpace.World.StarSystem.Runtime;

namespace GrimSpace.World.StarSystem.Effects;

public sealed class ActivateContractEffect : IEffect<StarMap, ActorRuntime>
{
	private readonly ContractState _state;

	public ActivateContractEffect(ContractState state) => _state = state;

	public IReadOnlyList<IRecord> Apply(StarMap world, ActorRuntime runtime, string actorId)
	{
		if (!world.ContractRegistry.Activate(_state))
			return [];

		if (world.ContractRegistry.TryGetState(_state.ContractId, out var activated))
			ContractDeliveryRoleSupport.OnContractActivated(world, activated);
		return [];
	}

	public void Undo(StarMap world, ActorRuntime runtime, string actorId)
	{
		ContractDeliveryRoleSupport.OnContractEnded(world, _state.ContractId);
		world.ContractRegistry.Deactivate(_state.ContractId);
	}
}
