using GrimSpace.Units;
using GrimSpace.Units.Enums;
using GrimSpace.Units.Loadouts.Abilities;
using GrimSpace.Units.Specs;

namespace GrimSpace.Tests;

internal static class CatalogExpectations
{
	private static ShipLoadout BattleLoadoutFor(EType chassis) =>
		chassis == EType.Fighter
			? ShipCatalog.FullFighterLoadout()
			: ShipCatalog.NewRunLoadoutFor(chassis);

	public static int UsesPerTurn(EType chassis, EAbilityKind kind) =>
		BattleLoadoutFor(chassis).InstalledAbilities
			.Where(installed => installed.Spec.Kind == kind)
			.Sum(installed => installed.Spec is IPerTurnAbility perTurn ? perTurn.UsesPerTurn : 0);

	public static int ScrapDroneSwarmDamage(EType chassis = EType.Fighter) =>
		DefaultScrapDroneSwarmSpec(chassis).Damage;

	public static int LightningCannonMaxReach(EType chassis = EType.Fighter) =>
		AbilityReach.MaxManhattanFromFirer(
			ShipCatalog.SpecFor(chassis).TryGetBaselineForKind(EAbilityKind.LightningCannon)!);

	public static LightningCannonSpec DefaultLightningCannonSpec(EType chassis = EType.Fighter) =>
		(LightningCannonSpec)ShipCatalog.SpecFor(chassis).TryGetBaselineForKind(EAbilityKind.LightningCannon)!;

	public static ScrapDroneSwarmSpec DefaultScrapDroneSwarmSpec(EType chassis = EType.Fighter) =>
		(ScrapDroneSwarmSpec)ShipCatalog.SpecFor(chassis).TryGetBaselineForKind(EAbilityKind.ScrapDroneSwarm)!;

	public static PatrolBaySpec DefaultPatrolBaySpec(EType chassis = EType.Carrier) =>
		(PatrolBaySpec)ShipCatalog.SpecFor(chassis).TryGetBaselineForKind(EAbilityKind.PatrolBay)!;

	public static TorpedoLauncherSpec DefaultTorpedoLauncherSpec(EType chassis = EType.Fighter) =>
		(TorpedoLauncherSpec)ShipCatalog.SpecFor(chassis).TryGetBaselineForKind(EAbilityKind.TorpedoLauncher)!;

	public static TorpedoLauncherSpec DefaultTorpedoLauncher() =>
		DefaultTorpedoLauncherSpec(EType.Fighter);
}
