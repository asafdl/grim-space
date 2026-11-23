using GrimSpace.Battle.Encounter;
using GrimSpace.Battle.Encounter.Generation;
using GrimSpace.Battle.NonUnits;
using GrimSpace.Battle.Spatial;
using GrimSpace.Math.Grid;
using GrimSpace.Units.Enums;

namespace GrimSpace.Tests.Encounter;

[BattleTestSuite]
public sealed class HazardPlacementTests
{
	[Fact]
	public void IonStormSpawn_CreatesPassableSingleCellHazard()
	{
		var cell = new Coord(5, 5, 5);
		var spawn = new BattleHazardSpawn
		{
			Kind = EBattleHazardKind.IonStorm,
			Origin = cell,
			Cells = new HashSet<Coord> { cell },
		};

		var hazard = spawn.CreateNonUnit(BattleTestFixture.Grid(12));

		Assert.IsType<IonStorm>(hazard);
		Assert.Equal(cell, Assert.Single(hazard.Cells));
		Assert.Empty(hazard.MovementBlockingCells);
		Assert.Empty(hazard.AbilityBlockingCells);
	}

	[Fact]
	public void IonStormPlacement_ScattersSevenOrEightDeterministicUnoccupiedCells()
	{
		var placement = new SingleCellHazardPlacement(EBattleHazardKind.IonStorm, requireComplete: true);

		for (var seed = 0; seed < 32; seed++)
		{
			var encounter = BattleEncounter.DevDefault(seed, gridSize: 64);
			var first = new HazardPlacementContext { Rng = new Random(seed), GridSize = 64, Spawns = encounter.Spawns };
			var second = new HazardPlacementContext { Rng = new Random(seed), GridSize = 64, Spawns = encounter.Spawns };
			var existing = encounter.WorldHazards.Where(h => h.Kind != EBattleHazardKind.Asteroid).ToArray();
			foreach (var hazard in existing)
			{
				first.ClaimedCells.UnionWith(hazard.Cells);
				second.ClaimedCells.UnionWith(hazard.Cells);
			}

			placement.Place(first);
			placement.Place(second);

			Assert.InRange(first.Placed.Count, 7, 8);
			Assert.Equal(first.Placed.Select(h => h.Origin), second.Placed.Select(h => h.Origin));
			Assert.All(first.Placed, storm =>
			{
				Assert.Equal(EBattleHazardKind.IonStorm, storm.Kind);
				Assert.Equal(storm.Origin, Assert.Single(storm.Cells));
				Assert.DoesNotContain(encounter.Spawns, ship => ship.Position == storm.Origin);
				Assert.DoesNotContain(existing, hazard => hazard.Cells.Contains(storm.Origin));
			});
			Assert.Equal(first.Placed.Count, first.Placed.Select(h => h.Origin).Distinct().Count());
		}
	}

	[Fact]
	public void IonStormPlacement_ReportsWhenNoCellsAreAvailable()
	{
		var encounter = BattleEncounter.DevDefault(seed: 0, gridSize: 64);
		var context = new HazardPlacementContext { Rng = new Random(0), GridSize = 64, Spawns = encounter.Spawns };
		for (var x = 0; x < context.GridSize; x++)
		for (var y = 0; y < context.GridSize; y++)
		for (var z = 0; z < context.GridSize; z++)
			context.ClaimedCells.Add(new Coord(x, y, z));

		var placement = new SingleCellHazardPlacement(EBattleHazardKind.IonStorm, requireComplete: true);

		Assert.Throws<InvalidOperationException>(() => placement.Place(context));
		Assert.Empty(context.Placed);
	}

	[Theory]
	[InlineData(12)]
	[InlineData(64)]
	public void DevEncounters_GenerateIonStormsWithoutOverlappingOtherHazards(int gridSize)
	{
		for (var seed = 0; seed < 32; seed++)
		{
			var encounter = BattleEncounter.DevDefault(seed, gridSize);
			var storms = encounter.WorldHazards.Where(h => h.Kind == EBattleHazardKind.IonStorm).ToArray();
			var otherHazards = encounter.WorldHazards.Where(h => h.Kind != EBattleHazardKind.IonStorm).ToArray();

			Assert.InRange(storms.Length, 7, 8);
			Assert.All(storms, storm =>
			{
				Assert.DoesNotContain(encounter.Spawns, ship => ship.Position == storm.Origin);
				Assert.DoesNotContain(otherHazards, hazard => hazard.Cells.Contains(storm.Origin));
			});
		}
	}

	[Fact]
	public void DevEncounters_ScatterSpacedRoughlyBalancedPlumesAroundTheFightAndKeepAsteroidsOffThem()
	{
		var counts = new HashSet<int>();
		for (var seed = 0; seed < 32; seed++)
		{
			var encounter = BattleEncounter.DevDefault(seed, gridSize: 64);
			var wells = encounter.WorldHazards.Where(hazard => hazard.Kind == EBattleHazardKind.GravityWell).ToArray();
			Assert.Equal(new Random(seed).Next(3), wells.Length);
			counts.Add(wells.Length);
			var repeated = BattleEncounter.DevDefault(seed, gridSize: 64);
			Assert.Equal(wells.Select(well => well.Origin), repeated.WorldHazards
				.Where(hazard => hazard.Kind == EBattleHazardKind.GravityWell).Select(well => well.Origin));
			foreach (var well in wells)
			{
				Assert.Equal(BodyFrame.WorldAligned(well.Origin), well.Frame);
				Assert.Equal(27, well.Cells.Count);
				Assert.True(well.Cells.SetEquals(Coord.OffsetsInCube(1).Select(offset => well.Origin + offset)));
				Assert.DoesNotContain(encounter.Spawns, spawn => well.Cells.Contains(spawn.Position));
			}
			var claimedCells = new HashSet<Coord>();
			foreach (var hazard in encounter.WorldHazards)
			{
				Assert.DoesNotContain(hazard.Cells, claimedCells.Contains);
				claimedCells.UnionWith(hazard.Cells);
			}
			var plumes = encounter.WorldHazards
				.Where(hazard => hazard.Kind == EBattleHazardKind.CryoPlume)
				.Select(hazard => Assert.Single(hazard.Cells))
				.ToArray();
			var ships = encounter.Spawns.Select(spawn => spawn.Position).ToArray();

			Assert.InRange(plumes.Length, SingleCellHazardPlacement.MinCount, SingleCellHazardPlacement.MaxCount);
			Assert.All(plumes, plume =>
			{
				Assert.All(ships, ship => Assert.True(plume.ChebyshevDistanceTo(ship) >= SingleCellHazardPlacement.MinShipDistance));
				Assert.InRange(plume.X, ships.Min(s => s.X) - SingleCellHazardPlacement.Padding, ships.Max(s => s.X) + SingleCellHazardPlacement.Padding);
				Assert.InRange(plume.Y, ships.Min(s => s.Y) - SingleCellHazardPlacement.Padding, ships.Max(s => s.Y) + SingleCellHazardPlacement.Padding);
				Assert.InRange(plume.Z, ships.Min(s => s.Z) - SingleCellHazardPlacement.Padding, ships.Max(s => s.Z) + SingleCellHazardPlacement.Padding);
			});
			for (var i = 0; i < plumes.Length; i++)
			for (var j = i + 1; j < plumes.Length; j++)
				Assert.True(plumes[i].ChebyshevDistanceTo(plumes[j]) >= SingleCellHazardPlacement.MinPlumeSpacing);

			var playerCenter = SingleCellHazardPlacement.Centroid(
				encounter.Spawns.Where(spawn => spawn.Team == ETeam.Player).Select(spawn => spawn.Position).ToArray());
			var enemyCenter = SingleCellHazardPlacement.Centroid(
				encounter.Spawns.Where(spawn => spawn.Team != ETeam.Player).Select(spawn => spawn.Position).ToArray());
			Assert.All(wells, well =>
			{
				Assert.InRange(well.Origin.X, System.Math.Min(playerCenter.X, enemyCenter.X), System.Math.Max(playerCenter.X, enemyCenter.X));
				Assert.InRange(well.Origin.Y, System.Math.Min(playerCenter.Y, enemyCenter.Y), System.Math.Max(playerCenter.Y, enemyCenter.Y));
				Assert.InRange(well.Origin.Z, System.Math.Min(playerCenter.Z, enemyCenter.Z), System.Math.Max(playerCenter.Z, enemyCenter.Z));
			});
			Assert.True(SingleCellHazardPlacement.SideImbalance(plumes, playerCenter, enemyCenter) <= SingleCellHazardPlacement.MaxSideImbalance);

			var plumeCells = plumes.ToHashSet();
			Assert.All(
				encounter.WorldHazards.Where(hazard => hazard.Kind == EBattleHazardKind.Asteroid),
				asteroid => Assert.DoesNotContain(asteroid.Cells, plumeCells.Contains));
		}
		Assert.True(counts.SetEquals([0, 1, 2]));
	}

	[Fact]
	public void GravityWellPlacement_SkipsOccupiedMidpointAndAlreadyClaimedCells()
	{
		var encounter = BattleEncounter.DevDefault(seed: 7, gridSize: 64);
		var playerCenter = SingleCellHazardPlacement.Centroid(encounter.Spawns
			.Where(spawn => spawn.Team == ETeam.Player).Select(spawn => spawn.Position).ToArray());
		var enemyCenter = SingleCellHazardPlacement.Centroid(encounter.Spawns
			.Where(spawn => spawn.Team == ETeam.Enemy).Select(spawn => spawn.Position).ToArray());
		var midpoint = new Coord(
			(playerCenter.X + enemyCenter.X) / 2,
			(playerCenter.Y + enemyCenter.Y) / 2,
			(playerCenter.Z + enemyCenter.Z) / 2);
		var context = new HazardPlacementContext
		{
			Rng = new Random(0),
			GridSize = 64,
			Spawns = encounter.Spawns,
		};
		context.ClaimedCells.Add(midpoint);

		new GravityWellPlacement().Place(context);

		Assert.Equal(2, context.Placed.Count);
		foreach (var well in context.Placed)
		{
			Assert.DoesNotContain(midpoint, well.Cells);
			Assert.DoesNotContain(encounter.Spawns, spawn => well.Cells.Contains(spawn.Position));
			Assert.All(well.Cells, cell =>
			{
				Assert.InRange(cell.X, 0, 63);
				Assert.InRange(cell.Y, 0, 63);
				Assert.InRange(cell.Z, 0, 63);
			});
		}
	}
}
