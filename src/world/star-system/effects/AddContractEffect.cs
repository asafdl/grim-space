using GrimSpace.Core.Actions;
using GrimSpace.Core.Engine;
using GrimSpace.World.StarSystem.Contracts;
using GrimSpace.World.StarSystem.Runtime;

namespace GrimSpace.World.StarSystem.Effects;

public sealed class AddContractEffect(Contract contract, int expiresAtTick) : IEffect<StarMap, ActorRuntime>
{
	public IReadOnlyList<IRecord> Apply(StarMap world, ActorRuntime runtime, string actorId)
	{
		world.ContractRegistry.TryAdd(contract, expiresAtTick);
		return [];
	}

	public void Undo(StarMap world, ActorRuntime runtime, string actorId) { }
}
