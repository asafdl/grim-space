using GrimSpace.Math;

namespace GrimSpace.Tests.Math;

public sealed class WeightedPickTests
{
	private static readonly double[] Weights = [1, 3, 0, 6];

	[Theory]
	[InlineData(0.00, 0)]
	[InlineData(0.099, 0)]
	[InlineData(0.10, 1)]
	[InlineData(0.399, 1)]
	[InlineData(0.40, 3)]
	[InlineData(0.999, 3)]
	public void Index_MapsUnitRollToCumulativeRanges(double roll, int expected) =>
		Assert.Equal(expected, WeightedPick.Index(Weights, weight => weight, roll));

	[Fact]
	public void Index_ZeroWeightItemIsNeverPicked()
	{
		var random = new StableRandom(7);
		var picks = Enumerable.Range(0, 1_000)
			.Select(_ => WeightedPick.Index(Weights, weight => weight, random.NextDouble()));

		Assert.DoesNotContain(2, picks);
	}

	[Fact]
	public void Of_ReturnsItemAtPickedIndex() =>
		Assert.Equal("heavy", WeightedPick.Of(
			["light", "heavy"],
			item => item == "heavy" ? 9 : 1,
			0.5));

	[Fact]
	public void Index_RejectsEmptyAndNonPositiveTotals()
	{
		Assert.Throws<ArgumentException>(() =>
			WeightedPick.Index(Array.Empty<double>(), weight => weight, 0.5));
		Assert.Throws<ArgumentException>(() =>
			WeightedPick.Index(new double[] { 0, 0 }, weight => weight, 0.5));
	}
}
