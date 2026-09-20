using GrimSpace.Units;
using GrimSpace.Units.Enums;
using GrimSpace.Units.Loadouts.Abilities;

namespace GrimSpace.Tests;

internal static class CatalogExpectations
{
	public static int UsesPerTurn(EType chassis, EAbilityKind kind) =>
		ShipCatalog.DefaultInstalledAbilitiesFor(chassis)
			.Where(installed => installed.Spec.Kind == kind)
			.Sum(installed => installed.Spec is IPerTurnAbility perTurn ? perTurn.UsesPerTurn : 0);

	public static int FlakDamage(EType chassis = EType.Fighter) =>
		DefaultFlakSpec(chassis).Damage;

	public static int RailgunMaxReach(EType chassis = EType.Fighter) =>
		AbilityReach.MaxManhattanFromFirer(
			ShipCatalog.DefaultAbilitySpec(chassis, EAbilityKind.Railgun)!);

	public static RailgunSpec DefaultRailgunSpec(EType chassis = EType.Fighter) =>
		(RailgunSpec)ShipCatalog.DefaultAbilitySpec(chassis, EAbilityKind.Railgun)!;

	public static FlakSpec DefaultFlakSpec(EType chassis = EType.Fighter) =>
		(FlakSpec)ShipCatalog.DefaultAbilitySpec(chassis, EAbilityKind.Flak)!;

	public static PatrolBaySpec DefaultPatrolBaySpec(EType chassis = EType.Carrier) =>
		(PatrolBaySpec)ShipCatalog.DefaultAbilitySpec(chassis, EAbilityKind.PatrolBay)!;

	public static TorpedoLauncherSpec DefaultTorpedoLauncherSpec(EType chassis = EType.Fighter) =>
		(TorpedoLauncherSpec)ShipCatalog.DefaultAbilitySpec(chassis, EAbilityKind.TorpedoLauncher)!;

	public static TorpedoBodySpec DefaultTorpedoBody() =>
		TorpedoBodySpec.Require(ShipCatalog.DefaultFor(EType.Torpedo));
}
