using GrimSpace.Math;

namespace GrimSpace.Tests.Math;

public sealed class StableRandomTests
{
	[Fact]
	public void TriangularWeightedNumber_IsDeterministicAndInRange()
	{
		var first = new StableRandom(42);
		var second = new StableRandom(42);

		var firstResults = Enumerable.Range(0, 100)
			.Select(_ => first.TriangularWeightedNumber(0, 10))
			.ToArray();
		var secondResults = Enumerable.Range(0, 100)
			.Select(_ => second.TriangularWeightedNumber(0, 10))
			.ToArray();

		Assert.Equal(firstResults, secondResults);
		Assert.All(firstResults, result => Assert.InRange(result, 0, 10));
	}

	[Fact]
	public void TriangularWeightedNumber_CenterIsMoreLikelyThanTail()
	{
		var random = new StableRandom(42);
		var results = Enumerable.Range(0, 10_000)
			.Select(_ => random.TriangularWeightedNumber(0, 10))
			.ToArray();

		Assert.True(results.Count(result => result == 5) > results.Count(result => result == 0));
	}

	[Fact]
	public void TriangularWeightedNumber_RejectsReversedRange()
	{
		Assert.Throws<ArgumentException>(() =>
			new StableRandom(42).TriangularWeightedNumber(2, 1));
	}
}
