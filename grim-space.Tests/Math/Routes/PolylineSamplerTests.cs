using GrimSpace.Math.Grid;
using GrimSpace.Math.Routes;

namespace GrimSpace.Tests.Math.Routes;

public sealed class PolylineSamplerTests
{
	[Fact]
	public void Sample_ReturnsPositionAndTangentAlongArcLength()
	{
		var points = new[]
		{
			new Coord(0, 0, 0),
			new Coord(10, 0, 0),
		};

		var (position, tangent) = PolylineSampler.Sample(points, 5);

		Assert.Equal(5, position.X);
		Assert.Equal(0, position.Z);
		Assert.True(tangent.X > 0);
		Assert.Equal(0, tangent.Z);
	}

	[Fact]
	public void Length_SumsSegmentDistances()
	{
		var points = new[]
		{
			new Coord(0, 0, 0),
			new Coord(3, 0, 4),
		};

		Assert.Equal(5, PolylineSampler.Length(points));
	}
}
