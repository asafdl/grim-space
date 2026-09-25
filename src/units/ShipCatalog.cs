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

	public static ShipLoadout NewRunLoadoutFor(EType chassis) =>
		chassis switch
		{
			EType.Fighter => StarterFighterLoadout(),
			EType.Carrier => SpecFor(chassis).NewDefaultLoadout(),
			EType.Patrol => SpecFor(chassis).NewDefaultLoadout(),
			EType.Torpedo => SpecFor(chassis).NewDefaultLoadout(),
			_ => throw new ArgumentOutOfRangeException(nameof(chassis), chassis, null),
		};

	public static ShipLoadout FullFighterLoadout() => FighterSpec.Instance.NewDefaultLoadout();

	public static ShipInstance CreateInstance(string id, EType chassis) =>
		ShipInstance.FromSpec(id, SpecFor(chassis), NewRunLoadoutFor(chassis));

	private static ShipLoadout StarterFighterLoadout()
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
