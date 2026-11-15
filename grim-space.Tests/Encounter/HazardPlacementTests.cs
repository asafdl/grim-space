using GrimSpace.Battle.Encounter;
using GrimSpace.Battle.Encounter.Generation;
using GrimSpace.Math.Grid;
using GrimSpace.Units.Enums;

namespace GrimSpace.Tests.Encounter;

[BattleTestSuite]
public sealed class HazardPlacementTests
{
	[Fact]
	public void DevEncounters_ScatterSpacedRoughlyBalancedPlumesAroundTheFightAndKeepAsteroidsOffThem()
	{
		for (var seed = 0; seed < 32; seed++)
		{
			var encounter = BattleEncounter.DevDefault(seed, gridSize: 64);
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
			Assert.True(CryoPlumePlacement.SideImbalance(plumes, playerCenter, enemyCenter) <= CryoPlumePlacement.MaxSideImbalance);

			var plumeCells = plumes.ToHashSet();
			Assert.All(
				encounter.WorldHazards.Where(hazard => hazard.Kind == EBattleHazardKind.Asteroid),
				asteroid => Assert.DoesNotContain(asteroid.Cells, plumeCells.Contains));
		}
	}
}
