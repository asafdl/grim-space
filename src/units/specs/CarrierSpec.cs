using GrimSpace.Math.Grid;
using GrimSpace.Units.Enums;
using GrimSpace.Units.Loadouts.Abilities;
using GrimSpace.Units.Loadouts.Defenses;

namespace GrimSpace.Units.Specs;

public sealed class CarrierSpec : ShipSpec
{
	public static CarrierSpec Instance { get; } = new();

	private CarrierSpec()
	{
	}

	public override EType Chassis => EType.Carrier;
	public override int DefaultMaxHullPoints => 2;

	public override FaceShieldPoints DefaultMaxShieldPoints
	{
		get
		{
			var profile = new FaceShieldPoints();
			profile.Fill(2);
			return profile;
		}
	}

	public override IReadOnlyList<WeaponSlot> Slots { get; } =
	[
		new(new(EAbilityKind.Railgun, ESpatialOrientation.Forward), ChassisWeaponBaselines.Railgun()),
		new(new(EAbilityKind.PatrolBay, ESpatialOrientation.Ventral), ChassisWeaponBaselines.PatrolBay(PatrolSpec.Instance)),
	];
}
