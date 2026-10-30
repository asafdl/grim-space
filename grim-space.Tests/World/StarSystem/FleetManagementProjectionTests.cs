using GrimSpace.Units;
using GrimSpace.Units.Enums;
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
		var shipId = run.GetFieldedShipIds()
			.Single(id => run.ItemInventory.Get(id).Spec.Chassis == EType.Fighter);
		Assert.True(run.PilotRegistry.TryGetForShip(shipId, out var pilot));

		var view = FleetManagementProjection.Collect(run);

		Assert.Equal(2, view.Ships.Count);
		var ship = Assert.Single(view.Ships, entry => entry.Id == shipId);
		Assert.Equal(shipId, ship.Id);
		Assert.Equal("Fighter", ship.Chassis);
		Assert.Equal(EShipGearTier.T0, ship.GearTier);
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
				[("DAMAGE", "200"), ("RANGE", "10"), ("USES PER TURN", "1")]));
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
		Assert.Equal(pilot.Id, view.Pilots.Single(entry => entry.Id == pilot.Id).Id);
		Assert.Equal(EOperatorClassId.SpaceRanger, pilot.ClassId);
		Assert.Equal(EOperatorClassId.SpaceRanger, ship.ClassId);
	}

	[Fact]
	public void Collect_ProjectsPilotAndShipClasses()
	{
		using var run = RunState.CreateNewRun(42);
		var fighterShipId = run.GetFieldedShipIds()
			.Single(id => run.ItemInventory.Get(id).Spec.Chassis == EType.Fighter);
		var pilot = run.PilotRegistry.TryGetForShip(fighterShipId, out var fighterPilot)
			? fighterPilot
			: throw new InvalidOperationException("Starter fighter pilot is missing.");
		const string minerShipId = "projection-unclassified-miner";
		Assert.True(run.TryAcquireOwnedShip(
			new ShipSpawnDeclaration(minerShipId, EType.RepurposedMiner)));

		var view = FleetManagementProjection.Collect(run);

		var fighterShip = Assert.Single(view.Ships, ship => ship.Id == fighterShipId);
		var minerShip = Assert.Single(view.Ships, ship => ship.Id == minerShipId);
		Assert.Equal(EOperatorClassId.SpaceRanger, pilot.ClassId);
		Assert.Equal(EOperatorClassId.SpaceRanger, fighterShip.ClassId);
		Assert.Equal(EOperatorClassId.Support, minerShip.ClassId);
	}

	[Fact]
	public void Collect_ProjectsPurchasedGearTierDistinctFromStarterT0()
	{
		using var run = RunState.CreateNewRun(42);
		const string purchasedId = "projection-t1-fighter";
		Assert.True(run.TryAcquireOwnedShip(
			new ShipSpawnDeclaration(purchasedId, EType.Fighter, EShipGearTier.T1)));

		var view = FleetManagementProjection.Collect(run);

		var purchased = Assert.Single(view.Ships, ship => ship.Id == purchasedId);
		Assert.Equal(EShipGearTier.T1, purchased.GearTier);
		Assert.Contains(view.Ships, ship => ship.GearTier == EShipGearTier.T0);
	}
}
