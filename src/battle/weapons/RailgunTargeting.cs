using GrimSpace.Battle.Spatial;
using GrimSpace.Battle.World;
using GrimSpace.Math.Grid;

namespace GrimSpace.Battle.Weapons;

public static class RailgunTargeting
{
	public static HashSet<Coord> GetBurstCells(
		BodyFrame frame,
		Func<Coord, bool> isInBounds)
	{
		var result = new HashSet<Coord>();

		for (var fore = 1; fore <= CombatConfig.RailgunLineLength; fore++)
		{
			var cell = frame.ToWorld(fore, 0, 0);
			if (isInBounds(cell))
				result.Add(cell);
		}

		for (var depth = 0; depth <= CombatConfig.RailgunPyramidRange; depth++)
		{
			var fore = CombatConfig.RailgunLineLength + depth;
			for (var port = -depth; port <= depth; port++)
			{
				for (var dorsal = -depth; dorsal <= depth; dorsal++)
				{
					if (System.Math.Abs(port) + System.Math.Abs(dorsal) > depth)
						continue;

					var cell = frame.ToWorld(fore, port, dorsal);
					if (isInBounds(cell))
						result.Add(cell);
				}
			}
		}

		return result;
	}

	public static bool IsValidBurst(BodyFrame frame, Func<Coord, bool> isInBounds) =>
		GetBurstCells(frame, isInBounds).Count > 0;

	public static bool BurstHitsAnyOpponent(BattleWorld world, string actorId, HashSet<Coord> cells) =>
		world.UnitsExcept(actorId).Any(unit => unit.State.IsAlive && cells.Contains(unit.State.Position));

	public static bool WouldHitOpponent(BattleWorld world, string actorId)
	{
		var actor = world.StateOf(actorId);
		if (actor.RailgunRemaining <= 0)
			return false;

		var frame = BodyFrame.From(actor);
		var cells = GetBurstCells(frame, world.Grid.IsInBounds);
		return BurstHitsAnyOpponent(world, actorId, cells);
	}
}
