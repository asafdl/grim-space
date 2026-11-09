using GrimSpace.Core.Actions;
using GrimSpace.Core.Engine;
using GrimSpace.World.StarSystem.Runtime;

namespace GrimSpace.World.StarSystem.Effects;

public sealed class PilotWalkedOutEffect(string pilotId) : IEffect<StarMap, ActorRuntime>
{
	public IReadOnlyList<IRecord> Apply(StarMap world, ActorRuntime runtime, string actorId) =>
		[new Record<string>(pilotId)];

	public void Undo(StarMap world, ActorRuntime runtime, string actorId)
	{
	}
}
