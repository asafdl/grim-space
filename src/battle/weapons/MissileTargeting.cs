using GrimSpace.Math.Grid;

namespace GrimSpace.Battle.Weapons;

public static class MissileTargeting
{
	public static HashSet<Coord> GetValidCells(
		Coord origin,
		Coord forward,
		Coord right,
		Coord up,
		MissileMountConfig config,
		Func<Coord, bool> isInBounds)
	{
		var result = new HashSet<Coord>();
		var basis = GridBasis.From(forward, up, right);

		foreach (var (localForward, localRight, localUp) in Manhattan.EnumerateShell(config.Range))
		{
			if (!PassesArc(localForward, localRight, localUp, config))
				continue;

			var cell = basis.ToWorldCell(origin, localForward, localRight, localUp);
			if (isInBounds(cell))
				result.Add(cell);
		}

		return result;
	}

	public static bool IsValidTarget(
		Coord origin,
		Coord forward,
		Coord right,
		Coord up,
		Coord center,
		MissileMountConfig config,
		Func<Coord, bool> isInBounds)
	{
		if (!isInBounds(center))
			return false;

		var basis = GridBasis.From(forward, up, right);
		var delta = center - origin;
		if (!basis.TryToLocal(delta, out var localForward, out var localRight, out var localUp))
			return false;

		if (Manhattan.L1Norm(localForward, localRight, localUp) != config.Range)
			return false;

		return PassesArc(localForward, localRight, localUp, config);
	}

	private static bool PassesArc(int localForward, int localRight, int localUp, MissileMountConfig config) =>
		localForward >= config.MinForward
		&& System.Math.Abs(localRight) <= config.MaxAbsRight
		&& localUp >= config.MinUp
		&& localUp <= config.MaxUp;
}
