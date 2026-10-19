using GrimSpace.Battle.Presentation;
using GrimSpace.Math.Grid;

namespace GrimSpace.Tests.Presentation;

[BattleTestSuite]
public sealed class BattleDistanceTests
{
	[Fact]
	public void SeparationUsesEuclideanDistanceAtTacticalScale()
	{
		var meters = BattleDistance.SeparationMeters(
			new Coord(1, 1, 1),
			new Coord(4, 5, 7));

		Assert.Equal(MathF.Sqrt(61f) * 1000f, meters, 3);
	}

	[Theory]
	[InlineData(0f, "0 m")]
	[InlineData(141.42f, "140 m")]
	[InlineData(999f, "1 km")]
	[InlineData(1000f, "1 km")]
	[InlineData(3045f, "3 km")]
	[InlineData(3150f, "3.2 km")]
	public void FormatUsesReadableRoundedUnits(float meters, string expected) =>
		Assert.Equal(expected, BattleDistance.Format(meters));

	[Fact]
	public void ApproximateCellsUsesTacticalScale() =>
		Assert.Equal("~7 km", BattleDistance.ApproximateCells(7));
}
