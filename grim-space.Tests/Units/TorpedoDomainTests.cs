using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Abilities;
using GrimSpace.Battle.Player;
using GrimSpace.Battle.Presentation.Ui;
using GrimSpace.Battle.Units;
using GrimSpace.Math.Grid;
using GrimSpace.Units;
using GrimSpace.Units.Enums;

namespace GrimSpace.Tests.Units;

public sealed class TorpedoDomainTests
{
	[Fact]
	public void TorpedoStatsAreConfigured()
	{
		var stats = Stats.ForType(EType.Torpedo);

		Assert.Equal(1, stats.MaxHullPoints);
		Assert.Equal(0, stats.MaxShieldPoints.MaxOnAnyFace);
		Assert.Equal(1, stats.MinPathApCost);
		Assert.Equal(3, stats.MaxAp);
	}

	[Fact]
	public void TorpedoCapabilitiesAreMoveAndDetonate()
	{
		var caps = Capabilities.For(EType.Torpedo);

		Assert.Equal([MoveDef.Instance, DetonateDef.Instance], caps);
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
		var spec = Assert.Single(AbilityHudCatalog.ForUnit(EType.Torpedo), entry => entry.Mode == EPlayerMode.Detonate);
		var unit = UnitDisplayState.Capture(new State
		{
			Id = "torpedo",
			Type = EType.Torpedo,
			Position = new Coord(5, 5, 5),
			Fore = Coord.Forward,
			Dorsal = Coord.Up,
			Starboard = Coord.Cross(Coord.Up, Coord.Forward),
			FuelRemaining = 2,
			Stats = Stats.ForType(EType.Torpedo),
		});

		var ready = AbilityHudCatalog.BuildState(spec, unit, new AbilityLegality(WeaponPeek.Empty, false, Detonate: true));
		var illegal = AbilityHudCatalog.BuildState(spec, unit, new AbilityLegality(WeaponPeek.Empty, false, Detonate: false));

		Assert.Equal(BattleHudCopy.DetonateTooltip, spec.Tooltip);
		Assert.Equal("res://assets/ui/abilities/detonate.svg", spec.IconPath);
		Assert.Equal("2/3", ready.Charges);
		Assert.True(ready.Enabled);
		Assert.False(illegal.Enabled);
	}
}
