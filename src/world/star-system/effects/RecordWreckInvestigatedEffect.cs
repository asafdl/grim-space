using GrimSpace.Core.Actions;
using GrimSpace.Core.Engine;
using GrimSpace.World.StarSystem.Runtime;

namespace GrimSpace.World.StarSystem.Effects;

public record WreckInvestigated(string contractId, string wreckageId);

public sealed class RecordWreckInvestigatedEffect(string contractId, string wreckageId)
	: IEffect<StarMap, ActorRuntime>
{
	public IReadOnlyList<IRecord> Apply(StarMap world, ActorRuntime runtime, string actorId) =>
		[new Record<WreckInvestigated>(new WreckInvestigated(contractId, wreckageId))];

	public void Undo(StarMap world, ActorRuntime runtime, string actorId)
	{
	}
}
