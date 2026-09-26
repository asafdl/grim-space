using GrimSpace.Core.Actions;
using GrimSpace.Core.Engine;
using GrimSpace.World.StarSystem.Contracts;
using GrimSpace.World.StarSystem.Runtime;

namespace GrimSpace.World.StarSystem.Effects;

public sealed class EndContractEffect(string contractId, EContractStatus status)
	: IEffect<StarMap, ActorRuntime>
{
	private ContractState? _previous;

	public IReadOnlyList<IRecord> Apply(StarMap world, ActorRuntime runtime, string actorId)
	{
		if (!world.ContractRegistry.TryGetState(contractId, out _previous))
			throw new InvalidOperationException($"Contract '{contractId}' has no runtime state.");

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
		ContractDeliveryRoleSupport.OnContractEnded(world, contractId);
		return [];
	}

	public void Undo(StarMap world, ActorRuntime runtime, string actorId)
	{
		if (_previous is null)
			throw new InvalidOperationException($"Contract '{contractId}' ending was not applied.");

		world.ContractRegistry.Restore(_previous);
		ContractDeliveryRoleSupport.OnContractActivated(world, _previous);
		_previous = null;
	}
}
