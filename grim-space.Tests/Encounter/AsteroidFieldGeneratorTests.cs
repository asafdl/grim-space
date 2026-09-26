using GrimSpace.Battle.Encounter.Generation;
using GrimSpace.Math.Grid;

namespace GrimSpace.Tests.Encounter;

[BattleTestSuite]
public sealed class AsteroidFieldGeneratorTests
{
	[Fact]
	public void Generate_ProducesCompactVolumesWithinFieldAndClearOfUnits()
	{
		var unit = new Coord(32, 32, 32);
		var config = new AsteroidFieldConfig
		{
			Seed = 42,
			UnitPositions = [unit],
		};

		var hazards = AsteroidFieldGenerator.Generate(config);

		Assert.NotEmpty(hazards);
		Assert.Contains(hazards, hazard => hazard.Cells.Count == 27);
		Assert.Contains(hazards, hazard => hazard.Cells.Count == 64);
		foreach (var hazard in hazards)
		{
			Assert.Contains(hazard.Origin, hazard.Cells);
			var xs = hazard.Cells.Select(cell => cell.X).Distinct().Order().ToArray();
			var ys = hazard.Cells.Select(cell => cell.Y).Distinct().Order().ToArray();
			var zs = hazard.Cells.Select(cell => cell.Z).Distinct().Order().ToArray();
			Assert.Equal(xs.Length, ys.Length);
			Assert.Equal(xs.Length, zs.Length);
			Assert.InRange(xs.Length, 1, 4);
			Assert.Equal(xs.Length * ys.Length * zs.Length, hazard.Cells.Count);
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
