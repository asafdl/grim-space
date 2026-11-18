using GrimSpace.Math.Grid;
using GrimSpace.Battle.World;

namespace GrimSpace.Battle.Ai;

// Cheap optimistic weapon reach for DFS bounds — not exact path/facing search.
internal static class OffensiveReach
{
	private static readonly Coord[] Steps =
		[new(1, 0, 0), new(-1, 0, 0), new(0, 1, 0), new(0, -1, 0), new(0, 0, 1), new(0, 0, -1)];

	public static int OptimisticMoveBubble(int actionPoints) =>
		actionPoints;

	public static bool CouldPossiblyDamage(Coord self, int actionPoints, Coord opponent, int weaponReach) =>
		self.ManhattanDistanceTo(opponent) <= OptimisticMoveBubble(actionPoints) + weaponReach;

	internal static Coord[] RelaxedReach(BattleWorld world, Coord origin, int budget)
	{
		var costs = new Dictionary<Coord, int> { [origin] = 0 };
		var queue = new PriorityQueue<Coord, int>();
		queue.Enqueue(origin, 0);
		while (queue.TryDequeue(out var cell, out var cost))
		{
			if (cost != costs[cell])
				continue;
			foreach (var step in Steps)
			{
				var next = cell + step;
				if (!world.Grid.IsInBounds(next))
					continue;
				// Ignore facing, occupancy and depth; assisted translations still cost zero.
				var nextCost = cost + (world.IsTranslationAssisted(cell, step) ? 0 : 1);
				if (nextCost > budget || costs.TryGetValue(next, out var previous) && previous <= nextCost)
					continue;
				costs[next] = nextCost;
				queue.Enqueue(next, nextCost);
			}
		}
		return costs.Keys.ToArray();
	}
}
