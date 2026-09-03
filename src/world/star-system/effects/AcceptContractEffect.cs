using GrimSpace.Core.Actions;
using GrimSpace.Core.Engine;
using GrimSpace.World.StarSystem.Runtime;

namespace GrimSpace.World.StarSystem.Effects;

public sealed class AcceptContractEffect : IEffect<StarMap, ActorRuntime>
{
	private readonly string _contractId;

	public AcceptContractEffect(string contractId) => _contractId = contractId;

	public IReadOnlyList<IRecord> Apply(StarMap world, ActorRuntime runtime, string actorId)
	{
		world.ContractRegistry.Accept(_contractId, actorId, world.Timeline.Clock.Current);
		return [];
	}

	public void Undo(StarMap world, ActorRuntime runtime, string actorId) { }
}
