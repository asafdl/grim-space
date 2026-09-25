using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Player;
using GrimSpace.Battle.Presentation.Ui;
using GrimSpace.Battle.Units;
using GrimSpace.Math.Grid;
using GrimSpace.Units;
using GrimSpace.Units.Enums;

namespace GrimSpace.Tests.Units;

[BattleTestSuite]
public sealed class TorpedoDomainTests
{
	[Fact]
	public void TorpedoStatsAreConfigured()
	{
		var stats = Stats.ForType(EType.Torpedo);
		var configuration = ShipCatalog.NewRunLoadoutFor(EType.Torpedo);

		var maxShields = configuration.MaxShieldPoints;

		Assert.Equal(1, configuration.MaxHullPoints);
		Assert.Equal(1, maxShields.MaxOnAnyFace);
		foreach (var face in Enum.GetValues<ESpatialOrientation>())
		{
			Assert.Equal(
				face == ESpatialOrientation.Retro ? 0 : 1,
				maxShields[face]);
		}
		Assert.Equal(CatalogExpectations.DefaultTorpedoLauncher().MovementActionPoints, stats.MaxAp);
	}

	[Fact]
	public void TorpedoCapabilitiesAreMoveAndDetonate()
	{
		var caps = Capabilities.For(EType.Torpedo);

		Assert.Equal([TorpedoMoveDef.Instance, DetonateDef.Instance], caps);
	}

	[Fact]
	public void TorpedoAbilitiesAreDetonateOnly()
	{
		var abilities = Capabilities.AbilitiesFor(EType.Torpedo);

		Assert.Single(abilities, def => def is DetonateDef);
	}

	[Fact]
	public void DetonateHudShowsFuelAndLegality()
	{
		var spec = Assert.Single(
			AbilityHudCatalog.ForUnit(EType.Torpedo),
			entry => entry.Mode == EPlayerMode.Detonate);
		var torpedoState = State.FromShipInstance(
			ShipInstance.FromCatalog("torpedo", EType.Torpedo),
			new Coord(5, 5, 5));
		torpedoState.FuelRemaining = 2;
		var unit = UnitDisplayState.Capture(torpedoState);

		var ready = AbilityHudCatalog.BuildState(
			spec,
			unit,
			new AbilityLegality(WeaponPeek.Empty, false, Detonate: true));
		var illegal = AbilityHudCatalog.BuildState(
			spec,
			unit,
			new AbilityLegality(WeaponPeek.Empty, false, Detonate: false));

		Assert.Equal(BattleHudCopy.DetonateTooltipFor(unit), spec.Tooltip(unit));
		Assert.Equal("res://assets/ui/abilities/detonate.svg", spec.IconPath);
		Assert.Equal("2/3", ready.Charges);
		Assert.True(ready.Enabled);
		Assert.False(illegal.Enabled);
	}
}
