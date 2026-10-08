using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Abilities;
using GrimSpace.Battle.Player;
using GrimSpace.Battle.Presentation.Ui;
using GrimSpace.Battle.Units;
using GrimSpace.Math.Grid;
using GrimSpace.Units;
using GrimSpace.Units.Enums;
using GrimSpace.Units.Loadouts.Abilities;

namespace GrimSpace.Tests.Units;

[BattleTestSuite]
public sealed class CarrierDomainTests
{
	[Fact]
	public void CarrierStatsAreConfigured()
	{
		var maneuverability = ShipCatalog.SpecFor(EType.Carrier).Maneuverability;
		var configuration = ShipCatalog.NewRunLoadoutFor(EType.Carrier);
		Assert.Equal(3, maneuverability.MaxActionPoints);
		Assert.Equal(2, configuration.MaxHullPoints);
		Assert.Equal(2, configuration.MaxShieldPoints.MaxOnAnyFace);
		Assert.Equal(0, CatalogExpectations.UsesPerTurn(EType.Carrier, EAbilityKind.ScrapDroneSwarm));
		Assert.Equal(1, CatalogExpectations.UsesPerTurn(EType.Carrier, EAbilityKind.LightningCannon));
	}

	[Fact]
	public void CarrierAbilitiesIncludeLightningCannonAndSpawnRepurposedMiner()
	{
		var abilities = Capabilities.AbilitiesFor(EType.Carrier);

		Assert.Contains(abilities, def => def is LightningCannonDef);
		Assert.Contains(abilities, def => def is SpawnRepurposedMinerDef);
		Assert.DoesNotContain(abilities, def => def is ScrapDroneSwarmDef);
	}

	[Fact]
	public void SpawnRepurposedMinerChargesShowOneWhenReady()
	{
		var unit = UnitDisplayState.Capture(
			State.FromShipInstance(
				ShipInstance.FromCatalog("carrier", EType.Carrier),
				new Coord(5, 5, 5)));

		var ready = AbilityHudCatalog.BuildState(
			AbilityHudCatalog.ForUnit(EType.Carrier)[1],
			unit,
			new AbilityLegality(WeaponPeek.Empty, SpawnRepurposedMiner: true, Detonate: false));
		var cooling = AbilityHudCatalog.BuildState(
			AbilityHudCatalog.ForUnit(EType.Carrier)[1],
			unit with
			{
				Mounts = unit.Mounts
					.Select(mount => mount.Mount.Kind == EAbilityKind.MinerBay
						? mount with
						{
							CooldownRemaining = CatalogExpectations.DefaultMinerBaySpec().CooldownTurns,
						}
						: mount)
					.ToList(),
			},
			new AbilityLegality(WeaponPeek.Empty, SpawnRepurposedMiner: true, Detonate: false));

		Assert.Equal("1/1", ready.Charges);
		Assert.Equal("0/1", cooling.Charges);
	}
}
