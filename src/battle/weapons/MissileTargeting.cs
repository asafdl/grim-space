using GrimSpace.Battle.Spatial;
using GrimSpace.Math.Grid;

namespace GrimSpace.Battle.Weapons;

public static class MissileTargeting
{
	public static HashSet<Coord> GetValidCells(
		BodyFrame frame,
		MissileMountConfig config,
		Func<Coord, bool> isInBounds)
	{
		var result = new HashSet<Coord>();

		foreach (var (localFore, localRight, localUp) in Manhattan.EnumerateShell(config.Range))
		{
			var port = -localRight;
			var dorsal = localUp;
			if (!PassesArc(localFore, port, dorsal, config))
				continue;

			var cell = frame.ToWorld(localFore, port, dorsal);
			if (isInBounds(cell))
				result.Add(cell);
		}

		return result;
	}

	public static bool IsValidTarget(
		BodyFrame frame,
		Coord center,
		MissileMountConfig config,
		Func<Coord, bool> isInBounds)
	{
		if (!isInBounds(center))
			return false;

		if (!frame.TryFromWorld(center, out var fore, out var port, out var dorsal))
			return false;

		if (Manhattan.L1Norm(fore, -port, dorsal) != config.Range)
			return false;

		return PassesArc(fore, port, dorsal, config);
	}

	private static bool PassesArc(int fore, int port, int dorsal, MissileMountConfig config) =>
		fore >= config.MinFore
		&& System.Math.Abs(port) <= config.MaxAbsPort
		&& dorsal >= config.MinDorsal
		&& dorsal <= config.MaxDorsal;
}
