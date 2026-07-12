using GrimSpace.Domain.Grid;

namespace GrimSpace.Domain.Combat;

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
		var range = config.Range;

		for (var localForward = -range; localForward <= range; localForward++)
		{
			for (var localRight = -range; localRight <= range; localRight++)
			{
				for (var localUp = -range; localUp <= range; localUp++)
				{
					if (Math.Abs(localForward) + Math.Abs(localRight) + Math.Abs(localUp) != range)
						continue;

					if (!PassesArc(localForward, localRight, localUp, config))
						continue;

					var cell = ToWorldCell(origin, forward, right, up, localForward, localRight, localUp);
					if (isInBounds(cell))
						result.Add(cell);
				}
			}
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

		var delta = center - origin;
		if (!TryToLocal(delta, forward, right, up, out var localForward, out var localRight, out var localUp))
			return false;

		if (Math.Abs(localForward) + Math.Abs(localRight) + Math.Abs(localUp) != config.Range)
			return false;

		return PassesArc(localForward, localRight, localUp, config);
	}

	private static bool PassesArc(int localForward, int localRight, int localUp, MissileMountConfig config) =>
		localForward >= config.MinForward
		&& Math.Abs(localRight) <= config.MaxAbsRight
		&& localUp >= config.MinUp
		&& localUp <= config.MaxUp;

	private static Coord ToWorldCell(
		Coord origin,
		Coord forward,
		Coord right,
		Coord up,
		int localForward,
		int localRight,
		int localUp) =>
		origin
		+ forward * localForward
		+ right * localRight
		+ up * localUp;

	private static bool TryToLocal(
		Coord delta,
		Coord forward,
		Coord right,
		Coord up,
		out int localForward,
		out int localRight,
		out int localUp)
	{
		localForward = Dot(delta, forward);
		localRight = Dot(delta, right);
		localUp = Dot(delta, up);
		return delta == ToWorldOffset(forward, right, up, localForward, localRight, localUp);
	}

	private static Coord ToWorldOffset(
		Coord forward,
		Coord right,
		Coord up,
		int localForward,
		int localRight,
		int localUp) =>
		forward * localForward
		+ right * localRight
		+ up * localUp;

	private static int Dot(Coord a, Coord b) =>
		a.X * b.X + a.Y * b.Y + a.Z * b.Z;
}
