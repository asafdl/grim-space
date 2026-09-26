using GrimSpace.Battle.Encounter.Generation;
using GrimSpace.Math.Grid;

namespace GrimSpace.Tests.Encounter;

[BattleTestSuite]
public sealed class AsteroidFieldGeneratorTests
{
	[Fact]
	public void Generate_ProducesLargeAsteroidsAndCompactVolumesWithinFieldAndClearOfUnits()
	{
		var unit = new Coord(32, 32, 32);
		var config = new AsteroidFieldConfig
		{
			Seed = 42,
			UnitPositions = [unit],
		};

		var hazards = AsteroidFieldGenerator.Generate(config);

		Assert.NotEmpty(hazards);
		Assert.InRange(hazards.Count(hazard => hazard.Cells.Count > 64), 3, 5);
		Assert.Contains(hazards, hazard => hazard.Cells.Count == 27);
		Assert.Contains(hazards, hazard => hazard.Cells.Count == 64);
		var bounds = new List<(Coord Min, Coord Max)>();
		foreach (var hazard in hazards)
		{
			Assert.Contains(hazard.Origin, hazard.Cells);
			var xs = hazard.Cells.Select(cell => cell.X).Distinct().Order().ToArray();
			var ys = hazard.Cells.Select(cell => cell.Y).Distinct().Order().ToArray();
			var zs = hazard.Cells.Select(cell => cell.Z).Distinct().Order().ToArray();
			if (hazard.Cells.Count > 64)
			{
				Assert.InRange(System.Math.Max(xs.Length, System.Math.Max(ys.Length, zs.Length)), 12, 15);
				Assert.InRange(System.Math.Min(xs.Length, System.Math.Min(ys.Length, zs.Length)), 5, 7);
			}
			else
			{
				Assert.Equal(xs.Length, ys.Length);
				Assert.Equal(xs.Length, zs.Length);
				Assert.InRange(xs.Length, 1, 4);
			}
			Assert.Equal(xs.Length * ys.Length * zs.Length, hazard.Cells.Count);
			bounds.Add((new Coord(xs[0], ys[0], zs[0]), new Coord(xs[^1], ys[^1], zs[^1])));
			Assert.All(hazard.Cells, cell =>
			{
				Assert.InRange(cell.X, config.RegionMargin, config.GridSize - config.RegionMargin - 1);
				Assert.InRange(cell.Y, config.RegionMargin, config.GridSize - config.RegionMargin - 1);
				Assert.InRange(cell.Z, config.RegionMargin, config.GridSize - config.RegionMargin - 1);
				Assert.True(System.Math.Max(
					System.Math.Max(System.Math.Abs(cell.X - unit.X), System.Math.Abs(cell.Y - unit.Y)),
					System.Math.Abs(cell.Z - unit.Z)) > config.UnitClearance);
			});
		}
		for (var i = 0; i < hazards.Count; i++)
		for (var j = i + 1; j < hazards.Count; j++)
		{
			var a = bounds[i];
			var b = bounds[j];
			Assert.True(a.Max.X + config.AsteroidGap < b.Min.X
				|| b.Max.X + config.AsteroidGap < a.Min.X
				|| a.Max.Y + config.AsteroidGap < b.Min.Y
				|| b.Max.Y + config.AsteroidGap < a.Min.Y
				|| a.Max.Z + config.AsteroidGap < b.Min.Z
				|| b.Max.Z + config.AsteroidGap < a.Min.Z);
		}
	}

	[Fact]
	public void Generate_UsesThreeToFiveLargeAsteroidsAcrossSeeds()
	{
		for (var seed = 0; seed < 16; seed++)
		{
			var hazards = AsteroidFieldGenerator.Generate(new AsteroidFieldConfig { Seed = seed });
			Assert.InRange(hazards.Count(hazard => hazard.Cells.Count > 64), 3, 5);
		}
	}

	[Fact]
	public void Generate_SameSeedProducesSameVolumes()
	{
		var config = new AsteroidFieldConfig { Seed = 119 };
		var first = AsteroidFieldGenerator.Generate(config);
		var second = AsteroidFieldGenerator.Generate(config);

		Assert.Equal(first.Count, second.Count);
		for (var i = 0; i < first.Count; i++)
		{
			Assert.Equal(first[i].Origin, second[i].Origin);
			Assert.True(first[i].Cells.ToHashSet().SetEquals(second[i].Cells));
		}
	}
}
