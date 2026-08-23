namespace GrimSpace.Math.Grid;

public static class GridBounds
{
	public static bool IsPointInRectangle(Coord point, int width, int height) =>
		point.Y == 0
		&& point.X >= 0 && point.X < width
		&& point.Z >= 0 && point.Z < height;

	public static bool IsCircleWhollyInRectangle(Coord center, int radius, int width, int height)
	{
		var edgePoints = new[]
		{
			center + new Coord(radius, 0, 0),
			center + new Coord(-radius, 0, 0),
			center + new Coord(0, 0, radius),
			center + new Coord(0, 0, -radius),
		};

		return edgePoints.All(point => IsPointInRectangle(point, width, height));
	}
}
