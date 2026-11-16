using GrimSpace.Battle.Encounter;
using GrimSpace.Battle.Encounter.Generation;
using GrimSpace.Battle.Spatial;
using GrimSpace.Math.Grid;
using GrimSpace.Units.Enums;

namespace GrimSpace.Tests.Encounter;

[BattleTestSuite]
public sealed class HazardPlacementTests
{
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

			Assert.InRange(plumes.Length, CryoPlumePlacement.MinCount, CryoPlumePlacement.MaxCount);
			Assert.All(plumes, plume =>
			{
				Assert.All(ships, ship => Assert.True(plume.ChebyshevDistanceTo(ship) >= CryoPlumePlacement.MinShipDistance));
				Assert.InRange(plume.X, ships.Min(s => s.X) - CryoPlumePlacement.Padding, ships.Max(s => s.X) + CryoPlumePlacement.Padding);
				Assert.InRange(plume.Y, ships.Min(s => s.Y) - CryoPlumePlacement.Padding, ships.Max(s => s.Y) + CryoPlumePlacement.Padding);
				Assert.InRange(plume.Z, ships.Min(s => s.Z) - CryoPlumePlacement.Padding, ships.Max(s => s.Z) + CryoPlumePlacement.Padding);
			});
			for (var i = 0; i < plumes.Length; i++)
			for (var j = i + 1; j < plumes.Length; j++)
				Assert.True(plumes[i].ChebyshevDistanceTo(plumes[j]) >= CryoPlumePlacement.MinPlumeSpacing);

			var playerCenter = CryoPlumePlacement.Centroid(
				encounter.Spawns.Where(spawn => spawn.Team == ETeam.Player).Select(spawn => spawn.Position).ToArray());
			var enemyCenter = CryoPlumePlacement.Centroid(
				encounter.Spawns.Where(spawn => spawn.Team != ETeam.Player).Select(spawn => spawn.Position).ToArray());
			Assert.All(wells, well =>
			{
				Assert.InRange(well.Origin.X, System.Math.Min(playerCenter.X, enemyCenter.X), System.Math.Max(playerCenter.X, enemyCenter.X));
				Assert.InRange(well.Origin.Y, System.Math.Min(playerCenter.Y, enemyCenter.Y), System.Math.Max(playerCenter.Y, enemyCenter.Y));
				Assert.InRange(well.Origin.Z, System.Math.Min(playerCenter.Z, enemyCenter.Z), System.Math.Max(playerCenter.Z, enemyCenter.Z));
			});
			Assert.True(CryoPlumePlacement.SideImbalance(plumes, playerCenter, enemyCenter) <= CryoPlumePlacement.MaxSideImbalance);

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
		var playerCenter = CryoPlumePlacement.Centroid(encounter.Spawns
			.Where(spawn => spawn.Team == ETeam.Player).Select(spawn => spawn.Position).ToArray());
		var enemyCenter = CryoPlumePlacement.Centroid(encounter.Spawns
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
