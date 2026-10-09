using GrimSpace.Math.Grid;
using GrimSpace.Units.Enums;
using GrimSpace.Units.Loadouts.Abilities;
using GrimSpace.Units.Loadouts.Defenses;
using GrimSpace.Units.Maneuvering;

namespace GrimSpace.Units.Specs;

public sealed class GunshipSpec : ShipSpec
{
	private static readonly LightningCannonSpec GunshipLightning = ExtendedLightning(
		ChassisWeaponBaselines.LightningCannon(),
		lineLengthBonus: 1,
		pyramidRangeBonus: 2);

	public static GunshipSpec Instance { get; } = new();

	private GunshipSpec()
	{
	}

	public override EType Chassis => EType.Gunship;
	public override int Initiative => 20;
	public override int DefaultMaxHullPoints => 2;
	public override ManeuverabilitySpec Maneuverability { get; } =
		ManeuverabilitySpec.StandardShip(3, 1);

	public override FaceShieldPoints DefaultMaxShieldPoints
	{
		get
		{
			var profile = new FaceShieldPoints();
			profile[ESpatialOrientation.Forward] = 1;
			profile[ESpatialOrientation.Retro] = 1;
			profile[ESpatialOrientation.Port] = 2;
			profile[ESpatialOrientation.Starboard] = 2;
			return profile;
		}
	}

	public override IReadOnlyList<WeaponSlot> Slots { get; } =
	[
		new(new(EAbilityKind.LightningCannon, ESpatialOrientation.Port), GunshipLightning),
		new(new(EAbilityKind.LightningCannon, ESpatialOrientation.Starboard), GunshipLightning),
	];

	private static LightningCannonSpec ExtendedLightning(
		LightningCannonSpec baseline,
		int lineLengthBonus,
		int pyramidRangeBonus) =>
		baseline with
		{
			LineLength = baseline.LineLength + lineLengthBonus,
			PyramidRange = baseline.PyramidRange + pyramidRangeBonus,
		};
}
