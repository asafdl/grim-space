using GrimSpace.Math.Grid;
using GrimSpace.Units.Enums;
using GrimSpace.Units.Loadouts.Abilities;
using GrimSpace.Units.Loadouts.Defenses;

namespace GrimSpace.Units;

public static class ShipCatalog
{
	public static ShipConfiguration DefaultFor(EType chassis) =>
		ShipConfiguration.Create(
			chassis,
			DefaultMaxHull(chassis),
			FaceShieldPoints.MaxFor(chassis),
			DefaultAbilityMountsFor(chassis));

	public static ShipSnapshot DefaultSnapshot(string shipId, EType chassis) =>
		ShipSnapshot.FromConfiguration(shipId, DefaultFor(chassis));

	public static IReadOnlyList<AbilityMount> DefaultAbilityMountsFor(EType chassis) =>
		chassis switch
		{
			EType.Fighter => DefaultFighterAbilityMounts(),
			EType.Carrier => DefaultCarrierAbilityMounts(),
			EType.Patrol => DefaultPatrolAbilityMounts(),
			EType.Torpedo => [],
			_ => throw new ArgumentOutOfRangeException(nameof(chassis), chassis, null),
		};

	private static int DefaultMaxHull(EType chassis) =>
		chassis switch
		{
			EType.Fighter => 2,
			EType.Carrier => 2,
			EType.Patrol => 1,
			EType.Torpedo => 1,
			_ => throw new ArgumentOutOfRangeException(nameof(chassis), chassis, null),
		};

	private static IReadOnlyList<AbilityMount> DefaultFighterAbilityMounts() =>
	[
		new AbilityMount(EAbilityKind.Flak, ESpatialOrientation.Port),
		new AbilityMount(EAbilityKind.Flak, ESpatialOrientation.Starboard),
		new AbilityMount(EAbilityKind.Railgun, ESpatialOrientation.Forward),
	];

	private static IReadOnlyList<AbilityMount> DefaultCarrierAbilityMounts() =>
	[
		new AbilityMount(EAbilityKind.Railgun, ESpatialOrientation.Forward),
	];

	private static IReadOnlyList<AbilityMount> DefaultPatrolAbilityMounts() =>
	[
		new AbilityMount(EAbilityKind.Flak, ESpatialOrientation.Starboard),
	];
}
