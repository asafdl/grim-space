using GrimSpace.Math.Grid;
using GrimSpace.World.StarSystem.Actions;
using GrimSpace.World.StarSystem.Runtime;
using GrimSpace.World.StarSystem.Units;

namespace GrimSpace.World.StarSystem.Contact;

internal static class WreckageContactRange
{
	public static bool IsWithinReachRange(
		StarMap world,
		ActorRuntime runtime,
		string actorId,
		Coord wreckPosition)
	{
		if (!world.FleetRegistry.TryGet(actorId, out var unit))
			return false;

		var position = MoveDef.ResolveOrigin(world, unit, runtime);
		return EngagementQueries.IsHunterInEngageRange(
			position,
			wreckPosition,
			unit.State.EngageRadius);
	}
}
