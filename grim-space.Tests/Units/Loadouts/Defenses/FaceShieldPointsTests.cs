using GrimSpace.Battle.Units;
using GrimSpace.Math.Grid;
using GrimSpace.Units;
using GrimSpace.Units.Enums;
using GrimSpace.Units.Loadouts.Defenses;

namespace GrimSpace.Tests.Units.Loadouts.Defenses;

public sealed class FaceShieldPointsTests
{
	[Fact]
	public void Catalog_Patrol_HasForwardShieldsOnly()
	{
		var defenses = ShipCatalog.DefaultFor(EType.Patrol).MaxShieldPoints;

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
		var defenses = ShipCatalog.DefaultFor(EType.Torpedo).MaxShieldPoints;

		foreach (var face in Enum.GetValues<ESpatialOrientation>())
			Assert.Equal(face == ESpatialOrientation.Retro ? 0 : 1, defenses[face]);
	}

	[Fact]
	public void Catalog_FighterAndCarrier_FillAllFaces()
	{
		foreach (var type in new[] { EType.Fighter, EType.Carrier })
		{
			var defenses = ShipCatalog.DefaultFor(type).MaxShieldPoints;
			foreach (var face in Enum.GetValues<ESpatialOrientation>())
				Assert.Equal(2, defenses[face]);
		}
	}

	[Fact]
	public void FromSpec_UsesConfiguredShieldCaps()
	{
		var maxShields = new FaceShieldPoints();
		maxShields[ESpatialOrientation.Dorsal] = 4;
		var defaults = ShipCatalog.DefaultFor(EType.Fighter);
		var spec = ShipSpec.Create(
			EType.Fighter,
			defaults.MaxHullPoints,
			maxShields,
			defaults.InstalledAbilities);

		var ship = ShipInstance.FromSpec("custom-fighter", spec);
		var state = State.FromShipInstance(ship, Coord.Zero);

		Assert.Equal(4, ship.ShieldPoints[ESpatialOrientation.Dorsal]);
		Assert.Equal(4, state.Spec.MaxShieldPoints[ESpatialOrientation.Dorsal]);
		Assert.Equal(4, state.ShieldPoints[ESpatialOrientation.Dorsal]);
		Assert.Equal(0, state.Spec.MaxShieldPoints[ESpatialOrientation.Forward]);
	}

	[Fact]
	public void BattleState_ClonesConfiguredShieldCaps()
	{
		var ship = ShipInstance.FromCatalog("fighter-a", EType.Fighter);
		var state = State.FromShipInstance(ship, Coord.Zero);

		ship.Spec.MaxShieldPoints[ESpatialOrientation.Forward] = 7;

		Assert.Equal(2, state.Spec.MaxShieldPoints[ESpatialOrientation.Forward]);
	}
}
