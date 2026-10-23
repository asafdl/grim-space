using GrimSpace.Units;
using GrimSpace.World.StarSystem.Presentation.Ui;
using RunState = GrimSpace.Run.State;

namespace GrimSpace.Tests.World.StarSystem;

[IntegrationTestSuite]
public sealed class FleetManagementProjectionTests
{
	[Fact]
	public void Collect_ProjectsOwnedShipPilotStatsAndAbilities()
	{
		using var run = RunState.CreateNewRun(42);
		var shipId = Assert.Single(run.GetFieldedShipIds());
		Assert.True(run.PilotRegistry.TryGetForShip(shipId, out var pilot));

		var view = FleetManagementProjection.Collect(run);

		var ship = Assert.Single(view.Ships);
		Assert.Equal(shipId, ship.Id);
		Assert.Equal("Fighter", ship.Chassis);
		Assert.Equal((2 * 100), ship.Hull);
		Assert.Equal((2 * 100), ship.MaxHull);
		Assert.Equal((3 * 100), ship.Shields);
		Assert.Equal((3 * 100), ship.MaxShields);
		Assert.Equal(4, ship.ActionPoints);
		Assert.Equal(3, ship.ManeuverPoints);
		Assert.Equal(40, ship.Initiative);
		Assert.Equal(pilot.Id, ship.Pilot?.Id);
		Assert.Equal(2, ship.Abilities.Count);
		Assert.Contains(
			ship.Abilities,
			ability => ability is
			{
				Kind: GrimSpace.Units.Loadouts.Abilities.EAbilityKind.LightningCannon,
				Name: "Lightning Cannon",
				Facets: "FORWARD",
			}
			&& ability.Stats.Select(stat => (stat.Label, stat.Value)).SequenceEqual(
				[("DAMAGE", "200"), ("RANGE", "9"), ("USES PER TURN", "1")]));
		Assert.Contains(
			ship.Abilities,
			ability => ability is
			{
				Kind: GrimSpace.Units.Loadouts.Abilities.EAbilityKind.VoidBombLauncher,
				Name: "Void Bomb Launcher",
				Facets: "VENTRAL",
			}
			&& ability.Stats.Select(stat => (stat.Label, stat.Value)).SequenceEqual(
				[("BLAST DAMAGE", "100"), ("BLAST RADIUS", "3"), ("FUEL TURNS", "2"), ("COOLDOWN", "3")]));
		Assert.Equal(pilot.Id, Assert.Single(view.Pilots).Id);
	}
}
