using GrimSpace.Math.Grid;

namespace GrimSpace.Tests.Math.Grid;

public sealed class GridRasterTests
{
	[Fact]
	public void FillCircle_StampsCellsWithinRadiusAndClipsBounds()
	{
		var visited = new HashSet<(int X, int Z)>();
		GridRaster.FillCircle(8, 8, new Coord(1, 0, 1), 1, (x, z) => visited.Add((x, z)));

		Assert.Contains((1, 1), visited);
		Assert.Contains((2, 1), visited);
		Assert.Contains((1, 2), visited);
		Assert.DoesNotContain((4, 4), visited);
		Assert.DoesNotContain((-1, 1), visited);
	}

	[Fact]
	public void StampCorridor_StampsCellsAlongPolyline()
	{
		var visited = new HashSet<(int X, int Z)>();
		GridRaster.StampCorridor(
			16,
			16,
			[new Coord(2, 0, 2), new Coord(6, 0, 2)],
			1,
			(x, z) => visited.Add((x, z)));

		Assert.Contains((2, 2), visited);
		Assert.Contains((4, 2), visited);
		Assert.Contains((6, 2), visited);
		Assert.DoesNotContain((4, 6), visited);
	}
}
