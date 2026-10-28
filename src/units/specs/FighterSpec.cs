using GrimSpace.Math.Grid;
using GrimSpace.Units.Enums;
using GrimSpace.Units.Loadouts.Abilities;
using GrimSpace.Units.Loadouts.Defenses;
using GrimSpace.Units.Maneuvering;

namespace GrimSpace.Units.Specs;

public sealed class FighterSpec : ShipSpec
{
	public static FighterSpec Instance { get; } = new();

	private FighterSpec()
	{
	}

	public override EType Chassis => EType.Fighter;
	public override EOperatorClassId ClassId => EOperatorClassId.SpaceRanger;
	public override int Initiative => 40;
	public override int DefaultMaxHullPoints => 200;
	public override ManeuverabilitySpec Maneuverability { get; } =
		ManeuverabilitySpec.StandardShip(4, 3);

	public override FaceShieldPoints DefaultMaxShieldPoints
	{
		get
		{
			var profile = new FaceShieldPoints();
			profile.Fill(200);
			return profile;
		}
	}

	public override IReadOnlyList<WeaponSlot> Slots { get; } =
	[
		new(new(EAbilityKind.ScrapDroneSwarm, ESpatialOrientation.Port)),
		new(new(EAbilityKind.ScrapDroneSwarm, ESpatialOrientation.Starboard)),
		new(new(EAbilityKind.LightningCannon, ESpatialOrientation.Forward)),
		new(new(EAbilityKind.GoopGun, ESpatialOrientation.Forward)),
		new(new(EAbilityKind.VoidBombLauncher, ESpatialOrientation.Retro)),
		new(new(EAbilityKind.VoidBombLauncher, ESpatialOrientation.Ventral)),
		new(new(EAbilityKind.VoidBombLauncher, ESpatialOrientation.Dorsal)),
	];

	public override ShipLoadout NewDefaultLoadout()
	{
		var shields = new FaceShieldPoints();
		shields[ESpatialOrientation.Forward] = 100;
		shields[ESpatialOrientation.Starboard] = 100;
		shields[ESpatialOrientation.Port] = 100;

		return ShipLoadout.Create(
			this,
			DefaultMaxHullPoints,
			shields,
			[
				new InstalledAbility(EAbilityKind.LightningCannon, ESpatialOrientation.Forward),
				new InstalledAbility(EAbilityKind.VoidBombLauncher, ESpatialOrientation.Ventral),
			]);
	}
}
