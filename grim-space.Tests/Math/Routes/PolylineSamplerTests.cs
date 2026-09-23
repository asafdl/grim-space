using GrimSpace.Math.Grid;
using GrimSpace.Math.Routes;

namespace GrimSpace.Tests.Math.Routes;

[BattleTestSuite]
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

	[Fact]
	public void SampleContinuous_InterpolatesFractionallyBetweenGridCells()
	{
		var points = new[]
		{
			new Coord(0, 0, 0),
			new Coord(10, 0, 0),
		};

		var sample = PolylineSampler.SampleContinuous(points, 2.5);

		Assert.Equal(2.5, sample.X);
		Assert.Equal(0, sample.Z);
		Assert.Equal(1, sample.NextPointIndex);
	}

	[Fact]
	public void SampleContinuous_InterpolatesFractionallyAlongDiagonal()
	{
		var points = new[]
		{
			new Coord(0, 0, 0),
			new Coord(3, 0, 4),
		};

		var sample = PolylineSampler.SampleContinuous(points, 2.5);

		Assert.Equal(1.5, sample.X);
		Assert.Equal(2, sample.Z);
		Assert.Equal(1, sample.NextPointIndex);
	}

	[Fact]
	public void Sample_RoundsContinuousSampleToIntegerCoord()
	{
		var points = new[]
		{
			new Coord(0, 0, 0),
			new Coord(10, 0, 0),
		};

		var (position, tangent) = PolylineSampler.Sample(points, 2.4);

		Assert.Equal(2, position.X);
		Assert.Equal(0, position.Z);
		Assert.True(tangent.X > 0);
		Assert.Equal(0, tangent.Z);
	}
}
