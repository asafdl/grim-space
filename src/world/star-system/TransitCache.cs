using GrimSpace.World.StarSystem.Pathfinding;
using GrimSpace.World.StarSystem.Units;

namespace GrimSpace.World.StarSystem;

public static class TransitCache
{
	public static void RebuildIfMissing(Unit unit, IPathfinder pathfinder)
	{
		if (unit.State.Phase != EPhase.InTransit)
		{
			unit.Runtime.CachedPath = null;
			return;
		}

		if (unit.Runtime.CachedPath is not null)
			return;

		var journey = unit.State.Journey;
		var result = pathfinder.FindPath(journey.Origin, journey.Destination);
		if (result is not PathfindingResult.Found found)
		{
			throw new InvalidOperationException(
				$"Unable to rebuild transit path for unit '{unit.State.Id}'.");
		}

		unit.Runtime.CachedPath = found.Path;
	}
}
