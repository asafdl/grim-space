using GrimSpace.Math.Grid;
using GrimSpace.Units.Enums;
using GrimSpace.Units.Loadouts.Abilities;
using GrimSpace.Units.Loadouts.Defenses;

namespace GrimSpace.Units.Specs;

public sealed class RepurposedMinerSpec : ShipSpec
{
	public static RepurposedMinerSpec Instance { get; } = new();

	private RepurposedMinerSpec()
	{
	}

	public override EType Chassis => EType.RepurposedMiner;
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
		new(new(EAbilityKind.ScrapDroneSwarm, ESpatialOrientation.Port), ChassisWeaponBaselines.ScrapDroneSwarm()),
		new(new(EAbilityKind.ScrapDroneSwarm, ESpatialOrientation.Starboard), ChassisWeaponBaselines.ScrapDroneSwarm()),
	];
}
