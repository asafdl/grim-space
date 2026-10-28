using GrimSpace.Math.Grid;
using GrimSpace.Units.Enums;
using GrimSpace.Units.Loadouts.Abilities;
using GrimSpace.Units.Loadouts.Defenses;
using GrimSpace.Units.Maneuvering;

namespace GrimSpace.Units.Specs;

public sealed class GunshipSpec : ShipSpec
{
	public static GunshipSpec Instance { get; } = new();

	private GunshipSpec()
	{
	}

	public override EType Chassis => EType.Gunship;
	public override EOperatorClassId ClassId => EOperatorClassId.Unclassified;
	public override int Initiative => 20;
	public override int DefaultMaxHullPoints => 200;
	public override ManeuverabilitySpec Maneuverability { get; } =
		ManeuverabilitySpec.StandardShip(3, 1);

	public override FaceShieldPoints DefaultMaxShieldPoints
	{
		get
		{
			var profile = new FaceShieldPoints();
			profile[ESpatialOrientation.Forward] = 100;
			profile[ESpatialOrientation.Retro] = 100;
			profile[ESpatialOrientation.Port] = 200;
			profile[ESpatialOrientation.Starboard] = 200;
			return profile;
		}
	}

	public override IReadOnlyList<WeaponSlot> Slots { get; } =
	[
		new(new(EAbilityKind.LightningCannon, ESpatialOrientation.Port)),
		new(new(EAbilityKind.LightningCannon, ESpatialOrientation.Starboard)),
	];
}
