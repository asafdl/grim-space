namespace GrimSpace.Math.Grid;

public static class GridRaster
{
	public static void FillCircle(
		int width,
		int height,
		Coord center,
		int radius,
		Action<int, int> visit)
	{
		ArgumentNullException.ThrowIfNull(visit);
		if (radius < 0)
			throw new ArgumentOutOfRangeException(nameof(radius));

		var minX = System.Math.Max(0, center.X - radius);
		var maxX = System.Math.Min(width - 1, center.X + radius);
		var minZ = System.Math.Max(0, center.Z - radius);
		var maxZ = System.Math.Min(height - 1, center.Z + radius);
		var radiusSquared = (long)radius * radius;

		for (var z = minZ; z <= maxZ; z++)
		{
			for (var x = minX; x <= maxX; x++)
			{
				var dx = x - center.X;
				var dz = z - center.Z;
				if (dx * (long)dx + dz * (long)dz <= radiusSquared)
					visit(x, z);
			}
		}
	}

	public static void StampCorridor(
		int width,
		int height,
		IReadOnlyList<Coord> centerline,
		int halfWidth,
		Action<int, int> visit)
	{
		ArgumentNullException.ThrowIfNull(visit);
		if (centerline.Count == 0)
			return;

		foreach (var point in centerline)
			FillCircle(width, height, point, halfWidth, visit);

		for (var i = 1; i < centerline.Count; i++)
			StampSegment(width, height, centerline[i - 1], centerline[i], halfWidth, visit);
	}

	private static void StampSegment(
		int width,
		int height,
		Coord start,
		Coord end,
		int halfWidth,
		Action<int, int> visit)
	{
		var x0 = start.X;
		var z0 = start.Z;
		var x1 = end.X;
		var z1 = end.Z;

		var dx = System.Math.Abs(x1 - x0);
		var dz = System.Math.Abs(z1 - z0);
		var sx = x0 < x1 ? 1 : -1;
		var sz = z0 < z1 ? 1 : -1;
		var error = dx - dz;

		while (true)
		{
			FillCircle(width, height, new Coord(x0, 0, z0), halfWidth, visit);

			if (x0 == x1 && z0 == z1)
				break;

			var error2 = error * 2;
			if (error2 > -dz)
			{
				error -= dz;
				x0 += sx;
			}

			if (error2 < dx)
			{
				error += dx;
				z0 += sz;
			}
		}
	}
}
