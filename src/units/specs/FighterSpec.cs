using GrimSpace.Math.Grid;
using GrimSpace.Units.Enums;
using GrimSpace.Units.Loadouts.Abilities;
using GrimSpace.Units.Loadouts.Defenses;

namespace GrimSpace.Units.Specs;

public sealed class FighterSpec : ShipSpec
{
	public static FighterSpec Instance { get; } = new();

	private FighterSpec()
	{
	}

	public override EType Chassis => EType.Fighter;
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
		new(new(EAbilityKind.Flak, ESpatialOrientation.Port), ChassisWeaponBaselines.Flak()),
		new(new(EAbilityKind.Flak, ESpatialOrientation.Starboard), ChassisWeaponBaselines.Flak()),
		new(new(EAbilityKind.Railgun, ESpatialOrientation.Forward), ChassisWeaponBaselines.Railgun()),
		new(new(EAbilityKind.TorpedoLauncher, ESpatialOrientation.Retro), ChassisWeaponBaselines.TorpedoLauncher()),
		new(new(EAbilityKind.TorpedoLauncher, ESpatialOrientation.Ventral), ChassisWeaponBaselines.TorpedoLauncher()),
		new(new(EAbilityKind.TorpedoLauncher, ESpatialOrientation.Dorsal), ChassisWeaponBaselines.TorpedoLauncher()),
	];
}
