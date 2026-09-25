using GrimSpace.Battle.Units;
using GrimSpace.Math.Grid;
using GrimSpace.Units;
using GrimSpace.Units.Enums;
using GrimSpace.Units.Loadouts.Defenses;

namespace GrimSpace.Tests.Units.Loadouts.Defenses;

[BattleTestSuite]
public sealed class FaceShieldPointsTests
{
	[Fact]
	public void Catalog_Patrol_HasForwardShieldsOnly()
	{
		var defenses = ShipCatalog.NewRunLoadoutFor(EType.Patrol).MaxShieldPoints;

		Assert.Equal(3, defenses[ESpatialOrientation.Forward]);
		Assert.Equal(0, defenses[ESpatialOrientation.Retro]);
		Assert.Equal(0, defenses[ESpatialOrientation.Dorsal]);
		Assert.Equal(0, defenses[ESpatialOrientation.Ventral]);
		Assert.Equal(0, defenses[ESpatialOrientation.Port]);
		Assert.Equal(0, defenses[ESpatialOrientation.Starboard]);
	}

	[Fact]
	public void FromSpawn_Patrol_ClonesForwardShieldProfile()
	{
		var state = State.FromShipInstance(
			ShipInstance.FromCatalog("patrol-1", EType.Patrol),
			Coord.Zero);

		Assert.Equal(3, state.ShieldPoints[ESpatialOrientation.Forward]);
		Assert.Equal(0, state.ShieldPoints[ESpatialOrientation.Retro]);
	}

	[Fact]
	public void Catalog_Torpedo_HasOneShieldOnEveryFaceExceptRetro()
	{
		var defenses = ShipCatalog.NewRunLoadoutFor(EType.Torpedo).MaxShieldPoints;

		foreach (var face in Enum.GetValues<ESpatialOrientation>())
			Assert.Equal(face == ESpatialOrientation.Retro ? 0 : 1, defenses[face]);
	}

	[Fact]
	public void Catalog_Carrier_FillsAllFaces()
	{
		var defenses = ShipCatalog.NewRunLoadoutFor(EType.Carrier).MaxShieldPoints;
		foreach (var face in Enum.GetValues<ESpatialOrientation>())
			Assert.Equal(2, defenses[face]);
	}

	[Fact]
	public void NewRun_Fighter_HasForwardPortAndStarboardShieldsOnly()
	{
		var defenses = ShipCatalog.NewRunLoadoutFor(EType.Fighter).MaxShieldPoints;

		Assert.Equal(1, defenses[ESpatialOrientation.Forward]);
		Assert.Equal(1, defenses[ESpatialOrientation.Port]);
		Assert.Equal(1, defenses[ESpatialOrientation.Starboard]);
		Assert.Equal(0, defenses[ESpatialOrientation.Retro]);
		Assert.Equal(0, defenses[ESpatialOrientation.Dorsal]);
		Assert.Equal(0, defenses[ESpatialOrientation.Ventral]);
	}

	[Fact]
	public void FromSpec_UsesConfiguredShieldCaps()
	{
		var maxShields = new FaceShieldPoints();
		maxShields[ESpatialOrientation.Dorsal] = 4;
		var defaults = ShipCatalog.NewRunLoadoutFor(EType.Fighter);
		var loadout = ShipLoadout.Create(
			GrimSpace.Units.Specs.FighterSpec.Instance,
			defaults.MaxHullPoints,
			maxShields,
			defaults.InstalledAbilities);

		var ship = ShipInstance.FromSpec("custom-fighter", GrimSpace.Units.Specs.FighterSpec.Instance, loadout);
		var state = State.FromShipInstance(ship, Coord.Zero);

		Assert.Equal(4, ship.ShieldPoints[ESpatialOrientation.Dorsal]);
		Assert.Equal(4, state.Loadout.MaxShieldPoints[ESpatialOrientation.Dorsal]);
		Assert.Equal(4, state.ShieldPoints[ESpatialOrientation.Dorsal]);
		Assert.Equal(0, state.Loadout.MaxShieldPoints[ESpatialOrientation.Forward]);
	}

	[Fact]
	public void BattleState_ClonesConfiguredShieldCaps()
	{
		var ship = ShipInstance.FromCatalog("fighter-a", EType.Fighter);
		var state = State.FromShipInstance(ship, Coord.Zero);

		ship.Loadout.MaxShieldPoints[ESpatialOrientation.Forward] = 7;

		Assert.Equal(2, state.Loadout.MaxShieldPoints[ESpatialOrientation.Forward]);
	}
}
