using System.Numerics;
using GrimSpace.Battle.Presentation.Graphics;
using GrimSpace.Math.Grid;

namespace GrimSpace.Tests.Presentation;

public sealed class CloudPatternTests
{
	[Theory]
	[InlineData(0)]
	[InlineData(1)]
	[InlineData(int.MaxValue)]
	public void SameSeedRecreatesTheSameRegions(int seed)
	{
		Assert.Equal(CloudPattern.Create(seed), CloudPattern.Create(seed));
	}

	[Fact]
	public void SeedsProduceDistinctCloudPatternsWithUsableCoverage()
	{
		var firstCenters = new HashSet<Vector3>();
		for (var seed = 0; seed < 32; seed++)
		{
			var regions = CloudPattern.Create(seed);
			firstCenters.Add(regions[0].Center);

			var selected = 0;
			for (var x = -5; x <= 5; x++)
			for (var y = -5; y <= 5; y++)
			for (var z = -5; z <= 5; z++)
			{
				var point = new Vector3(x, y, z) / 10f;
				if (CloudPattern.FindRegion(point, regions) >= 0)
					selected++;
			}

			Assert.InRange(selected, 20, 500);
			Assert.Equal(-1, CloudPattern.FindRegion(new Vector3(2f, 2f, 2f), regions));
		}

		Assert.Equal(32, firstCenters.Count);
	}

	[Fact]
	public void RegionSelectionUsesTheClosestContainingRegion()
	{
		CloudRegion[] regions =
		[
			new(new Vector3(0.2f, 0, 0), Vector3.One, Vector3.Zero, Vector3.Zero),
			new(Vector3.Zero, Vector3.One, Vector3.Zero, Vector3.Zero),
		];

		Assert.Equal(1, CloudPattern.FindRegion(Vector3.Zero, regions));
	}

	[Fact]
	public void NegativeSeedsAreRejected()
	{
		Assert.Throws<ArgumentOutOfRangeException>(() => CloudPattern.Create(-1));
	}

	[Theory]
	[InlineData(0)]
	[InlineData(int.MinValue)]
	[InlineData(int.MaxValue)]
	public void EncounterAndCellProduceStableUsableSeeds(int encounterSeed)
	{
		var cell = new Coord(4, 8, 12);
		var seed = CloudPattern.SeedFor(encounterSeed, cell);

		Assert.Equal(seed, CloudPattern.SeedFor(encounterSeed, cell));
		Assert.NotEqual(seed, CloudPattern.SeedFor(encounterSeed, new Coord(4, 8, 13)));
		Assert.NotEqual(seed, CloudPattern.SeedFor(encounterSeed ^ 1, cell));
		Assert.NotEmpty(CloudPattern.Create(seed));
	}
}
