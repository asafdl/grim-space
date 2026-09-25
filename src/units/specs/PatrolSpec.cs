using GrimSpace.Math.Grid;
using GrimSpace.Units.Enums;
using GrimSpace.Units.Loadouts.Abilities;
using GrimSpace.Units.Loadouts.Defenses;

namespace GrimSpace.Units.Specs;

public sealed class PatrolSpec : ShipSpec
{
	public static PatrolSpec Instance { get; } = new();

	private PatrolSpec()
	{
	}

	public override EType Chassis => EType.Patrol;
	public override int DefaultMaxHullPoints => 1;

	public override FaceShieldPoints DefaultMaxShieldPoints
	{
		get
		{
			var profile = new FaceShieldPoints();
			profile[ESpatialOrientation.Forward] = 3;
			return profile;
		}
	}

	public override IReadOnlyList<WeaponSlot> Slots { get; } =
	[
		new(new(EAbilityKind.Flak, ESpatialOrientation.Port), ChassisWeaponBaselines.Flak()),
		new(new(EAbilityKind.Flak, ESpatialOrientation.Starboard), ChassisWeaponBaselines.Flak()),
	];
}
