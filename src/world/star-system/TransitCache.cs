using GrimSpace.World.StarSystem.Pathfinding;
using GrimSpace.World.StarSystem.Runtime;
using GrimSpace.World.StarSystem.Units;

namespace GrimSpace.World.StarSystem;

public static class TransitCache
{
	public static void RebuildIfMissing(Unit unit, ActorRuntime runtime, IPathfinder pathfinder)
	{
		if (unit.State.Phase != EPhase.InTransit)
		{
			runtime.CachedPath = null;
			return;
		}

		if (runtime.CachedPath is not null)
			return;

		var journey = unit.State.Journey;
		var result = pathfinder.FindPath(journey.Origin, journey.Destination);
		if (result is not PathfindingResult.Found found)
		{
			throw new InvalidOperationException(
				$"Unable to rebuild transit path for unit '{unit.State.Id}'.");
		}

		runtime.CachedPath = found.Path;
	}
}
