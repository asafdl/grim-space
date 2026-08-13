using GrimSpace.Battle.Spatial;
using GrimSpace.Battle.Units;
using GrimSpace.Battle.Abilities;
using GrimSpace.Math.Grid;

namespace GrimSpace.Battle.World;

public enum EUnitRelation
{
	Self,
	Ally,
	Opponent,
}

public readonly record struct UnitInArea(Unit Unit, EUnitRelation Relation);

public static class WeaponBursts
{
	public static HashSet<Coord> RailgunBurstCells(
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

	public static bool IsValidRailgunBurst(BodyFrame frame, Func<Coord, bool> isInBounds) =>
		RailgunBurstCells(frame, isInBounds).Count > 0;

	public static HashSet<Coord> FlakBurstCells(
		BodyFrame frame,
		ESpatialOrientation mountedOn,
		Func<Coord, bool> isInBounds)
	{
		var result = new HashSet<Coord>();
		var (apexPort, outwardStep) = FlakBurstAxes(mountedOn);
		var range = CombatConfig.FlakRange;

		for (var outward = 0; outward <= range; outward++)
		{
			for (var fore = -outward; fore <= outward; fore++)
			{
				for (var dorsal = -outward; dorsal <= outward; dorsal++)
				{
					if (System.Math.Abs(fore) + System.Math.Abs(dorsal) > outward)
						continue;

					var port = apexPort + outwardStep * outward;
					var cell = frame.ToWorld(fore, port, dorsal);
					if (isInBounds(cell))
						result.Add(cell);
				}
			}
		}

		return result;
	}

	public static bool IsValidFlakBurst(
		BodyFrame frame,
		ESpatialOrientation mountedOn,
		Func<Coord, bool> isInBounds) =>
		FlakBurstCells(frame, mountedOn, isInBounds).Count > 0;

	public static ESpatialOrientation? FlakMountedOnForCell(BodyFrame frame, Coord cell)
	{
		if (!frame.TryFromWorld(cell, out _, out var port, out _))
			return null;

		if (port > 0)
			return ESpatialOrientation.Port;

		if (port < 0)
			return ESpatialOrientation.Starboard;

		return null;
	}

	private static (int apexPort, int outwardStep) FlakBurstAxes(ESpatialOrientation mountedOn) =>
		mountedOn switch
		{
			ESpatialOrientation.Port => (1, 1),
			ESpatialOrientation.Starboard => (-1, -1),
			_ => throw new ArgumentOutOfRangeException(nameof(mountedOn), mountedOn, null),
		};
}
