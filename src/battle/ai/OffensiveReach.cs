using GrimSpace.Battle.Movement;
using GrimSpace.Math.Grid;

namespace GrimSpace.Battle.Ai;

// Cheap optimistic weapon reach for DFS bounds — not exact path/facing search.
internal static class OffensiveReach
{
	public static int OptimisticMoveBubble(int actionPoints) =>
		actionPoints + MomentumConfig.ForLevel(MomentumConfig.MaxLevel).FreeForwardSteps;

	public static bool CouldPossiblyDamage(Coord self, int actionPoints, Coord opponent, int weaponReach) =>
		self.ManhattanDistanceTo(opponent) <= OptimisticMoveBubble(actionPoints) + weaponReach;
}
