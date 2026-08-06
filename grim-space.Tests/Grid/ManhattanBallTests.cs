using GrimSpace.Math.Grid;

namespace GrimSpace.Tests.Grid;

public sealed class ManhattanBallTests
{
	[Fact]
	public void EnumerateBallIncludesCenterAndExcludesL1BeyondRadius()
	{
		var center = new Coord(2, 2, 2);
		var cells = Manhattan.EnumerateBall(center, radius: 1).ToHashSet();

		Assert.Contains(center, cells);
		Assert.Contains(center + Coord.Forward, cells);
		Assert.DoesNotContain(center + Coord.Forward * 2, cells);
		Assert.DoesNotContain(new Coord(3, 3, 2), cells);
	}
}
