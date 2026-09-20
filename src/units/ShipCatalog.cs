using GrimSpace.Math.Grid;
using GrimSpace.Units.Enums;
using GrimSpace.Units.Loadouts.Abilities;
using GrimSpace.Units.Loadouts.Defenses;

namespace GrimSpace.Units;

public static class ShipCatalog
{
	private const int FlakDamage = 1;
	private const int FlakRange = 2;
	private const int FlaksPerTurn = 1;
	private const int RailgunDamage = 3;
	private const int RailgunLineLength = 8;
	private const int RailgunPyramidRange = 2;
	private const int RailgunsPerTurn = 1;
	private const int PatrolCooldownTurns = 2;
	private const int MaxLivingPatrolChildren = 5;
	private const int TorpedoLauncherCooldownTurns = 3;
	private const int TorpedoFuelTurns = 3;
	private const int TorpedoMovementActionPoints = 4;
	private const int TorpedoForwardMoveApCost = 1;
	private const int TorpedoLateralMoveApCost = 2;
	private const int TorpedoBlastRadius = 4;
	private const int TorpedoBlastDamage = 3;

	public static ShipSpec DefaultFor(EType chassis) =>
		ShipSpec.Create(
			chassis,
			DefaultMaxHull(chassis),
			DefaultInstalledAbilitiesFor(chassis),
			chassis == EType.Torpedo ? DefaultTorpedoBody() : null);

	public static FaceShieldPoints MaxShieldPointsFor(EType chassis) =>
		DefaultDefensesFor(chassis);

	public static AbilitySpec? DefaultAbilitySpec(EType chassis, EAbilityKind kind)
	{
		foreach (var installed in DefaultInstalledAbilitiesFor(chassis))
		{
			if (installed.Spec.Kind == kind)
				return installed.Spec;
		}

		return null;
	}

	public static IReadOnlyList<InstalledAbility> DefaultInstalledAbilitiesFor(EType chassis) =>
		chassis switch
		{
			EType.Fighter => DefaultFighterInstalledAbilities(),
			EType.Carrier => DefaultCarrierInstalledAbilities(),
			EType.Patrol => DefaultPatrolInstalledAbilities(),
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

	private static FaceShieldPoints DefaultDefensesFor(EType chassis)
	{
		var profile = new FaceShieldPoints();
		switch (chassis)
		{
			case EType.Fighter:
			case EType.Carrier:
				profile.Fill(2);
				break;
			case EType.Patrol:
				profile[ESpatialOrientation.Forward] = 3;
				break;
			case EType.Torpedo:
				profile.Fill(1);
				profile[ESpatialOrientation.Retro] = 0;
				break;
			default:
				throw new ArgumentOutOfRangeException(nameof(chassis), chassis, null);
		}

		return profile;
	}

	private static FlakSpec FlakSpec() => new(FlaksPerTurn, FlakDamage, FlakRange);

	private static RailgunSpec RailgunSpec() =>
		new(RailgunsPerTurn, RailgunDamage, RailgunLineLength, RailgunPyramidRange);

	private static PatrolBaySpec PatrolBaySpec() =>
		new(PatrolCooldownTurns, DefaultFor(EType.Patrol), MaxLivingPatrolChildren);

	private static TorpedoBodySpec DefaultTorpedoBody() =>
		new(
			TorpedoFuelTurns,
			TorpedoMovementActionPoints,
			TorpedoForwardMoveApCost,
			TorpedoLateralMoveApCost,
			TorpedoBlastRadius,
			TorpedoBlastDamage);

	private static TorpedoLauncherSpec TorpedoLauncherSpec() =>
		new(TorpedoLauncherCooldownTurns, DefaultFor(EType.Torpedo));

	private static IReadOnlyList<InstalledAbility> DefaultFighterInstalledAbilities() =>
	[
		new InstalledAbility(
			FlakSpec(),
			[ESpatialOrientation.Port, ESpatialOrientation.Starboard]),
		new InstalledAbility(
			RailgunSpec(),
			[ESpatialOrientation.Forward]),
		new InstalledAbility(
			TorpedoLauncherSpec(),
			[
				ESpatialOrientation.Retro,
				ESpatialOrientation.Ventral,
				ESpatialOrientation.Dorsal,
			]),
	];

	private static IReadOnlyList<InstalledAbility> DefaultCarrierInstalledAbilities() =>
	[
		new InstalledAbility(RailgunSpec(), [ESpatialOrientation.Forward]),
		new InstalledAbility(PatrolBaySpec(), [ESpatialOrientation.Ventral]),
	];

	private static IReadOnlyList<InstalledAbility> DefaultPatrolInstalledAbilities() =>
	[
		new InstalledAbility(
			FlakSpec(),
			[ESpatialOrientation.Port, ESpatialOrientation.Starboard]),
	];
}
