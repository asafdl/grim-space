using GrimSpace.Battle.Encounter;
using GrimSpace.Battle.Encounter.Generation;
using GrimSpace.Battle.Ai;
using GrimSpace.Battle.NonUnits;
using GrimSpace.Battle.Player;
using GrimSpace.Battle.Spatial;
using GrimSpace.Math.Grid;
using GrimSpace.Units.Enums;

namespace GrimSpace.Tests.Encounter;

[BattleTestSuite]
public sealed class HazardPlacementTests
{
	[Fact]
	public void GravityWellPlacement_ProvidesSpacedApproachAndFlankingOptions()
	{
		var player = new Coord(20, 32, 32);
		var enemy = new Coord(40, 32, 32);
		var spawns = new[]
		{
			BattleSpawnTestKit.Create("player", EType.Fighter, ETeam.Player, player, new UserExecutionAgent()),
			BattleSpawnTestKit.Create("enemy", EType.Fighter, ETeam.Enemy, enemy, new AiController()),
		};

		for (var seed = 0; seed < 32; seed++)
		{
			var context = new HazardPlacementContext { Rng = new Random(seed), GridSize = 64, Spawns = spawns };
			new GravityWellPlacement().Place(context);

			Assert.Equal(4, context.Placed.Count);
			Assert.Contains(context.Placed, well => well.Origin.Y >= player.Y + 3);
			Assert.Contains(context.Placed, well => well.Origin.Y <= player.Y - 3);
			Assert.Contains(context.Placed, well =>
				System.Math.Abs(well.Origin.Y - player.Y) >= 3
				|| System.Math.Abs(well.Origin.Z - player.Z) >= 3);
			Assert.Contains(context.Placed, well => Coord.Dot(well.Origin - player, well.Origin - player) <= 64);
			Assert.Contains(context.Placed, well => Coord.Dot(well.Origin - enemy, well.Origin - enemy) <= 64);
			foreach (var well in context.Placed)
			{
				Assert.InRange(well.Origin.X, player.X, enemy.X);
				Assert.InRange(well.Origin.Y, player.Y - 9, player.Y + 9);
				Assert.InRange(well.Origin.Z, player.Z - 9, player.Z + 9);
				Assert.DoesNotContain(spawns, spawn => well.Cells.Contains(spawn.Position));
			}
			for (var i = 0; i < context.Placed.Count; i++)
			for (var j = i + 1; j < context.Placed.Count; j++)
			{
				var delta = context.Placed[i].Origin - context.Placed[j].Origin;
				Assert.True(Coord.Dot(delta, delta) >= 36);
			}
		}
	}

	[Fact]
	public void SingleCellHazards_ConcentrateCoverNearTheEngagementAndHelpersAlongTheApproach()
	{
		var player = new Coord(20, 32, 32);
		var enemy = new Coord(40, 32, 32);
		var midpoint = new Coord(30, 32, 32);
		var spawns = new[]
		{
			BattleSpawnTestKit.Create("player", EType.Fighter, ETeam.Player, player, new UserExecutionAgent()),
			BattleSpawnTestKit.Create("enemy", EType.Fighter, ETeam.Enemy, enemy, new AiController()),
		};

		for (var seed = 0; seed < 16; seed++)
		{
			var context = new HazardPlacementContext { Rng = new Random(seed), GridSize = 64, Spawns = spawns };
			foreach (var kind in new[] { EBattleHazardKind.CryoPlume, EBattleHazardKind.IonStorm, EBattleHazardKind.AsteroidBelt })
				new SingleCellHazardPlacement(kind, required: true, count: kind == EBattleHazardKind.AsteroidBelt ? 30 : 10).Place(context);

			var cover = context.Placed.Where(hazard => hazard.Kind == EBattleHazardKind.AsteroidBelt).ToArray();
			Assert.Equal(30, cover.Length);
			Assert.Contains(cover, hazard => hazard.Origin.X < midpoint.X);
			Assert.Contains(cover, hazard => hazard.Origin.X > midpoint.X);
			Assert.All(cover, hazard =>
			{
				var offset = hazard.Origin - midpoint;
				Assert.True(Coord.Dot(offset, offset) <= 121);
			});
			for (var i = 0; i < cover.Length; i++)
			for (var j = i + 1; j < cover.Length; j++)
			{
				var delta = cover[i].Origin - cover[j].Origin;
				Assert.True(Coord.Dot(delta, delta) >= 16);
			}

			foreach (var kind in new[] { EBattleHazardKind.CryoPlume, EBattleHazardKind.IonStorm })
			{
				var helpers = context.Placed.Where(hazard => hazard.Kind == kind).ToArray();
				Assert.Equal(10, helpers.Length);
				Assert.All(helpers, hazard =>
				{
					Assert.InRange(hazard.Origin.X, player.X, enemy.X);
					var y = hazard.Origin.Y - player.Y;
					var z = hazard.Origin.Z - player.Z;
					Assert.True(y * y + z * z <= 49);
				});
			}
		}
	}

	[Fact]
	public void RequiredCover_WithLimitedSpaceKeepsFourKilometersBetweenCenters()
	{
		var spawns = new[]
		{
			BattleSpawnTestKit.Create("player", EType.Fighter, ETeam.Player, new Coord(5, 6, 6), new UserExecutionAgent()),
			BattleSpawnTestKit.Create("enemy", EType.Fighter, ETeam.Enemy, new Coord(15, 6, 6), new AiController()),
		};
		var available = new HashSet<Coord> { new(8, 6, 6), new(12, 6, 6) };
		var context = new HazardPlacementContext { Rng = new Random(0), GridSize = 24, Spawns = spawns };
		for (var x = 0; x < context.GridSize; x++)
		for (var y = 0; y < context.GridSize; y++)
		for (var z = 0; z < context.GridSize; z++)
		{
			var cell = new Coord(x, y, z);
			if (!available.Contains(cell))
				context.ClaimedCells.Add(cell);
		}

		new SingleCellHazardPlacement(EBattleHazardKind.AsteroidBelt, required: true, count: 2).Place(context);

		Assert.Equal(2, context.Placed.Count);
		Assert.True(available.SetEquals(context.Placed.Select(hazard => hazard.Origin)));
	}

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
	public void IonStormPlacement_ScattersTenDeterministicUnoccupiedCells()
	{
		var placement = new SingleCellHazardPlacement(EBattleHazardKind.IonStorm, required: true, count: 10);

		for (var seed = 0; seed < 32; seed++)
		{
			var encounter = BattleEncounter.DevDefault(seed);
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

			Assert.Equal(10, first.Placed.Count);
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
		var encounter = BattleEncounter.DevDefault(seed: 0);
		var context = new HazardPlacementContext { Rng = new Random(0), GridSize = 64, Spawns = encounter.Spawns };
		for (var x = 0; x < context.GridSize; x++)
		for (var y = 0; y < context.GridSize; y++)
		for (var z = 0; z < context.GridSize; z++)
			context.ClaimedCells.Add(new Coord(x, y, z));

		var placement = new SingleCellHazardPlacement(EBattleHazardKind.IonStorm, required: true, count: 10);

		Assert.Throws<InvalidOperationException>(() => placement.Place(context));
		Assert.Empty(context.Placed);
	}

	[Fact]
	public void AsteroidBeltPlacement_OmitsCoverWhenNoCellsAreAvailable()
	{
		var encounter = BattleEncounter.DevDefault(seed: 0);
		var context = new HazardPlacementContext
		{
			Rng = new Random(0),
			GridSize = 64,
			Spawns = encounter.Spawns,
		};
		for (var x = 0; x < context.GridSize; x++)
		for (var y = 0; y < context.GridSize; y++)
		for (var z = 0; z < context.GridSize; z++)
			context.ClaimedCells.Add(new Coord(x, y, z));

		var placement = new SingleCellHazardPlacement(
			EBattleHazardKind.AsteroidBelt,
			required: false,
			count: 30);

		placement.Place(context);

		Assert.Empty(context.Placed);
	}

	[Fact]
	public void DevEncounters_GenerateIonStormsWithoutOverlappingOtherHazards()
	{
		for (var seed = 0; seed < 32; seed++)
		{
			var encounter = BattleEncounter.DevDefault(seed);
			var storms = encounter.WorldHazards.Where(h => h.Kind == EBattleHazardKind.IonStorm).ToArray();
			var otherHazards = encounter.WorldHazards.Where(h => h.Kind != EBattleHazardKind.IonStorm).ToArray();

			Assert.Equal(4, encounter.WorldHazards.Count(h => h.Kind == EBattleHazardKind.GravityWell));
			Assert.Equal(10, storms.Length);
			Assert.All(storms, storm =>
			{
				Assert.DoesNotContain(encounter.Spawns, ship => ship.Position == storm.Origin);
				Assert.DoesNotContain(otherHazards, hazard => hazard.Cells.Contains(storm.Origin));
			});
		}
	}

	[Fact]
	public void DevEncounters_GenerateAsteroidBeltsDeterministicallyWithoutOverlaps()
	{
		for (var seed = 0; seed < 32; seed++)
		{
			var encounter = BattleEncounter.DevDefault(seed);
			var repeat = BattleEncounter.DevDefault(seed);
			var belts = encounter.WorldHazards
				.Where(hazard => hazard.Kind == EBattleHazardKind.AsteroidBelt)
				.ToArray();
			var repeatedBelts = repeat.WorldHazards
				.Where(hazard => hazard.Kind == EBattleHazardKind.AsteroidBelt)
				.ToArray();

			Assert.Equal(belts.Select(belt => (belt.Origin, belt.DamageResistancePercent)),
				repeatedBelts.Select(belt => (belt.Origin, belt.DamageResistancePercent)));
			Assert.Equal(30, belts.Length);
			for (var i = 0; i < belts.Length; i++)
			for (var j = i + 1; j < belts.Length; j++)
			{
				var delta = belts[i].Origin - belts[j].Origin;
				Assert.True(Coord.Dot(delta, delta) >= 16);
			}
			Assert.All(belts, belt =>
			{
				Assert.Contains(
					belt.DamageResistancePercent,
					new int?[] { AsteroidBelt.LowResistancePercent, AsteroidBelt.HighResistancePercent });
				Assert.Equal(belt.Origin, Assert.Single(belt.Cells));
				Assert.DoesNotContain(encounter.Spawns, spawn => spawn.Position == belt.Origin);
				Assert.Empty(belt.CreateNonUnit(BattleTestFixture.Grid(20)).MovementBlockingCells);
				Assert.Empty(belt.CreateNonUnit(BattleTestFixture.Grid(20)).AbilityBlockingCells);
			});

			var otherHazards = encounter.WorldHazards
				.Where(hazard => hazard.Kind != EBattleHazardKind.AsteroidBelt)
				.ToArray();
			Assert.All(belts, belt =>
				Assert.DoesNotContain(otherHazards, hazard => hazard.Cells.Contains(belt.Origin)));

			var claimedCells = new HashSet<Coord>();
			foreach (var hazard in encounter.WorldHazards)
			{
				Assert.DoesNotContain(hazard.Cells, claimedCells.Contains);
				claimedCells.UnionWith(hazard.Cells);
			}

			var asteroids = encounter.WorldHazards
				.Where(hazard => hazard.Kind == EBattleHazardKind.Asteroid)
				.ToArray();
			Assert.All(belts, belt =>
				Assert.DoesNotContain(asteroids, asteroid => asteroid.Cells.Contains(belt.Origin)));
			Assert.Equal(10, encounter.WorldHazards.Count(hazard => hazard.Kind == EBattleHazardKind.IonStorm));
			Assert.Equal(10, encounter.WorldHazards.Count(hazard => hazard.Kind == EBattleHazardKind.CryoPlume));
			Assert.Contains(belts, belt => belt.DamageResistancePercent == AsteroidBelt.LowResistancePercent);
			Assert.Contains(belts, belt => belt.DamageResistancePercent == AsteroidBelt.HighResistancePercent);
		}
	}

	[Fact]
	public void DevEncounters_ScatterSpacedRoughlyBalancedPlumesAroundTheFightAndKeepAsteroidsOffThem()
	{
		for (var seed = 0; seed < 32; seed++)
		{
			var encounter = BattleEncounter.DevDefault(seed);
			var wells = encounter.WorldHazards.Where(hazard => hazard.Kind == EBattleHazardKind.GravityWell).ToArray();
			Assert.Equal(4, wells.Length);
			var repeated = BattleEncounter.DevDefault(seed);
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

			Assert.Equal(10, plumes.Length);
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
			Assert.True(SingleCellHazardPlacement.SideImbalance(plumes, playerCenter, enemyCenter) <= SingleCellHazardPlacement.MaxSideImbalance);

			var plumeCells = plumes.ToHashSet();
			Assert.All(
				encounter.WorldHazards.Where(hazard => hazard.Kind == EBattleHazardKind.Asteroid),
				asteroid => Assert.DoesNotContain(asteroid.Cells, plumeCells.Contains));
		}
	}

	[Fact]
	public void GravityWellPlacement_SkipsOccupiedMidpointAndAlreadyClaimedCells()
	{
		var encounter = BattleEncounter.DevDefault(seed: 7);
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

		Assert.Equal(4, context.Placed.Count);
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
