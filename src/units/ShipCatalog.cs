using GrimSpace.Math.Grid;
using GrimSpace.Units.Enums;
using GrimSpace.Units.Loadouts.Abilities;
using GrimSpace.Units.Loadouts.Defenses;
using GrimSpace.Units.Specs;

namespace GrimSpace.Units;

public static class ShipCatalog
{
	public static ShipSpec SpecFor(EType chassis) =>
		chassis switch
		{
			EType.Fighter => FighterSpec.Instance,
			EType.Carrier => CarrierSpec.Instance,
			EType.Patrol => PatrolSpec.Instance,
			EType.Torpedo => TorpedoSpec.Instance,
			_ => throw new ArgumentOutOfRangeException(nameof(chassis), chassis, null),
		};

	public static ShipLoadout LoadoutForTier(EType chassis, EShipGearTier tier, int? rollSeed = null)
	{
		var baseline = BaselineLoadoutFor(chassis);
		if (tier == EShipGearTier.T0)
			return baseline;

		return ShipLoadoutTierRoller.Roll(chassis, tier, rollSeed, baseline);
	}

	public static ShipLoadout NewRunLoadoutFor(EType chassis) =>
		LoadoutForTier(chassis, EShipGearTier.T0);

	public static ShipLoadout FullFighterLoadout() => FighterSpec.Instance.NewDefaultLoadout();

	public static ShipInstance CreateInstance(string id, EType chassis) =>
		ShipInstance.FromSpec(id, SpecFor(chassis), LoadoutForTier(chassis, EShipGearTier.T0));

	private static ShipLoadout BaselineLoadoutFor(EType chassis) =>
		chassis switch
		{
			EType.Fighter => BaselineFighterLoadout(),
			EType.Carrier or EType.Patrol or EType.Torpedo => SpecFor(chassis).NewDefaultLoadout(),
			_ => throw new ArgumentOutOfRangeException(nameof(chassis), chassis, null),
		};

	private static ShipLoadout BaselineFighterLoadout()
	{
		var spec = FighterSpec.Instance;
		var shields = new FaceShieldPoints();
		shields[ESpatialOrientation.Forward] = 1;
		shields[ESpatialOrientation.Starboard] = 1;
		shields[ESpatialOrientation.Port] = 1;

		var installed = new[]
		{
			new InstalledAbility(ChassisWeaponBaselines.StarterRailgun(), ESpatialOrientation.Forward),
			new InstalledAbility(
				ChassisWeaponBaselines.StarterTorpedoLauncher(),
				ESpatialOrientation.Ventral),
		};

		return ShipLoadout.Create(spec, spec.DefaultMaxHullPoints, shields, installed);
	}
}
