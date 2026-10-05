using GrimSpace.Core.Actions;
using GrimSpace.Core.Engine;
using GrimSpace.Core.Log;
using GrimSpace.World.StarSystem.Units;
using GrimSpace.World.StarSystem.Runtime;

namespace GrimSpace.World.StarSystem.Effects;

public sealed class RecordHostileContactIgnoreEffect(
	string targetId,
	int ignoreUntilTick) : IEffect<StarMap, ActorRuntime>
{
	public IReadOnlyList<IRecord> Apply(
		StarMap world,
		ActorRuntime runtime,
		string actorId)
	{
		runtime.IgnoreUntilTickByTargetId[targetId] = ignoreUntilTick;
		if (world.FleetRegistry.TryGet(actorId, out var actor)
			&& actor.State.Type == EType.PirateFleet)
			GameLog.Log(
				$"[star-map] hostile reaction actor={actorId} target={targetId} result=ignored " +
				$"ignoreUntilTick={ignoreUntilTick}");
		return [];
	}

	public void Undo(StarMap world, ActorRuntime runtime, string actorId) { }
}
