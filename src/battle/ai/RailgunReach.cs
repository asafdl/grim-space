using GrimSpace.Battle.Movement;
using GrimSpace.Battle.Weapons;
using GrimSpace.Math.Grid;

namespace GrimSpace.Battle.Ai;

// Cheap optimistic railgun reach for DFS bounds — not exact path/facing search.
internal static class RailgunReach
{
	public static int OptimisticMoveBubble(int actionPoints) =>
		actionPoints + MomentumConfig.ForLevel(MomentumConfig.MaxLevel).FreeForwardSteps;

	public static bool CouldPossiblyHit(Coord self, int actionPoints, Coord opponent)
	{
		var dist = self.ManhattanDistanceTo(opponent);
		return dist <= OptimisticMoveBubble(actionPoints) + CombatConfig.MaxRailgunManhattanRange;
	}
}
