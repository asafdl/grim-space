using GrimSpace.Math.Grid;
using GrimSpace.Units.Enums;
using GrimSpace.Units.Loadouts.Abilities;
using GrimSpace.Units.Loadouts.Defenses;
using GrimSpace.Units.Maneuvering;

namespace GrimSpace.Units.Specs;

public sealed class RogueSpec : ShipSpec
{
	public static RogueSpec Instance { get; } = new();

	private RogueSpec()
	{
	}

	public override EType Chassis => EType.Rogue;
	public override EOperatorClassId ClassId => EOperatorClassId.Rogue;
	public override int Initiative => 50;
	public override int DefaultMaxHullPoints => 120;
	public override ManeuverabilitySpec Maneuverability { get; } =
		ManeuverabilitySpec.StandardShip(4, 4, reverseApCost: 1);

	public override FaceShieldPoints DefaultMaxShieldPoints
	{
		get
		{
			var profile = new FaceShieldPoints();
			profile.Fill(50);
			profile[ESpatialOrientation.Retro] = 0;
			return profile;
		}
	}

	public override IReadOnlyList<WeaponSlot> Slots { get; } =
	[
		new(new(EAbilityKind.HighSpeedRod, ESpatialOrientation.Ventral)),
		new(new(EAbilityKind.HighSpeedRod, ESpatialOrientation.Dorsal)),
		new(new(EAbilityKind.RoadsWeDontNeedRoads, ESpatialOrientation.Forward)),
	];
}
