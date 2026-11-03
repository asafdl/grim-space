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
	public void Catalog_RepurposedMiner_HasForwardShieldsOnly()
	{
		var defenses = ShipCatalog.NewRunLoadoutFor(EType.RepurposedMiner).BaseShieldPoints;

		Assert.Equal(300, defenses[ESpatialOrientation.Forward]);
		Assert.Equal(0, defenses[ESpatialOrientation.Retro]);
		Assert.Equal(0, defenses[ESpatialOrientation.Dorsal]);
		Assert.Equal(0, defenses[ESpatialOrientation.Ventral]);
		Assert.Equal(0, defenses[ESpatialOrientation.Port]);
		Assert.Equal(0, defenses[ESpatialOrientation.Starboard]);
	}

	[Fact]
	public void FromSpawn_RepurposedMiner_ClonesForwardShieldProfile()
	{
		var state = State.FromShipInstance(
			ShipInstance.FromCatalog("repurposed-miner-1", EType.RepurposedMiner),
			Coord.Zero);

		Assert.Equal(300, state.CurrentShieldPoints[ESpatialOrientation.Forward]);
		Assert.Equal(0, state.CurrentShieldPoints[ESpatialOrientation.Retro]);
	}

	[Fact]
	public void Catalog_Torpedo_HasOneShieldOnEveryFaceExceptRetro()
	{
		var defenses = ShipCatalog.NewRunLoadoutFor(EType.VoidBomb).BaseShieldPoints;

		foreach (var face in Enum.GetValues<ESpatialOrientation>())
			Assert.Equal(face == ESpatialOrientation.Retro ? 0 : 100, defenses[face]);
	}

	[Fact]
	public void Catalog_Carrier_FillsAllFaces()
	{
		var defenses = ShipCatalog.NewRunLoadoutFor(EType.Carrier).BaseShieldPoints;
		foreach (var face in Enum.GetValues<ESpatialOrientation>())
			Assert.Equal(200, defenses[face]);
	}

	[Fact]
	public void NewRun_Fighter_HasForwardPortAndStarboardShieldsOnly()
	{
		var defenses = ShipCatalog.NewRunLoadoutFor(EType.Fighter).BaseShieldPoints;

		Assert.Equal(100, defenses[ESpatialOrientation.Forward]);
		Assert.Equal(100, defenses[ESpatialOrientation.Port]);
		Assert.Equal(100, defenses[ESpatialOrientation.Starboard]);
		Assert.Equal(0, defenses[ESpatialOrientation.Retro]);
		Assert.Equal(0, defenses[ESpatialOrientation.Dorsal]);
		Assert.Equal(0, defenses[ESpatialOrientation.Ventral]);
	}

	[Fact]
	public void FromSpec_UsesConfiguredShieldCaps()
	{
		var maxShields = new FaceShieldPoints();
		maxShields[ESpatialOrientation.Dorsal] = 400;
		var defaults = ShipCatalog.NewRunLoadoutFor(EType.Fighter);
		var loadout = ShipLoadout.Create(
			GrimSpace.Units.Specs.FighterSpec.Instance,
			defaults.BaseHullPoints,
			maxShields,
			defaults.InstalledAbilities);

		var ship = ShipInstance.FromSpec("custom-fighter", GrimSpace.Units.Specs.FighterSpec.Instance, loadout);
		var state = State.FromShipInstance(ship, Coord.Zero);

		Assert.Equal(400, ship.CurrentShieldPoints[ESpatialOrientation.Dorsal]);
		Assert.Equal(400, state.Loadout.BaseShieldPoints[ESpatialOrientation.Dorsal]);
		Assert.Equal(400, state.CurrentShieldPoints[ESpatialOrientation.Dorsal]);
		Assert.Equal(0, state.Loadout.BaseShieldPoints[ESpatialOrientation.Forward]);
	}

	[Fact]
	public void BattleState_ClonesConfiguredShieldCaps()
	{
		var ship = ShipInstance.FromCatalog("fighter-a", EType.Fighter);
		var expected = ship.Loadout.BaseShieldPoints[ESpatialOrientation.Forward];
		var state = State.FromShipInstance(ship, Coord.Zero);

		ship.Loadout.BaseShieldPoints[ESpatialOrientation.Forward] = 700;

		Assert.Equal(expected, state.Loadout.BaseShieldPoints[ESpatialOrientation.Forward]);
	}
}
