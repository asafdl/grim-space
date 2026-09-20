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
		var defenses = ShipCatalog.MaxShieldPointsFor(EType.Patrol);

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
		var defenses = ShipCatalog.MaxShieldPointsFor(EType.Torpedo);

		foreach (var face in Enum.GetValues<ESpatialOrientation>())
			Assert.Equal(face == ESpatialOrientation.Retro ? 0 : 1, defenses[face]);
	}

	[Fact]
	public void Catalog_FighterAndCarrier_FillAllFaces()
	{
		foreach (var type in new[] { EType.Fighter, EType.Carrier })
		{
			var defenses = ShipCatalog.MaxShieldPointsFor(type);
			foreach (var face in Enum.GetValues<ESpatialOrientation>())
				Assert.Equal(2, defenses[face]);
		}
	}
}
