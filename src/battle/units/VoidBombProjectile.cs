using GrimSpace.Math.Grid;
using GrimSpace.Units.Loadouts.Abilities;
using GrimSpace.Units.Specs;

namespace GrimSpace.Battle.Units;

/// <summary>
/// Launch-time snapshot of the firing mount's torpedo parameters. Authoritative for spawned torpedo behavior in battle.
/// </summary>
public sealed record VoidBombProjectile(
	int FuelTurns,
	int MovementActionPoints,
	int ForwardMoveApCost,
	int LateralMoveApCost,
	int BlastRadius,
	int BlastDamage)
{
	public int? MoveApCost(ESpatialOrientation direction) =>
		direction switch
		{
			ESpatialOrientation.Forward => ForwardMoveApCost,
			ESpatialOrientation.Port
				or ESpatialOrientation.Starboard
				or ESpatialOrientation.Dorsal
				or ESpatialOrientation.Ventral => LateralMoveApCost,
			_ => null,
		};

	public static VoidBombProjectile FromLauncher(VoidBombLauncherSpec launcher) =>
		new(
			launcher.FuelTurns,
			launcher.MovementActionPoints,
			launcher.ForwardMoveApCost,
			launcher.LateralMoveApCost,
			launcher.BlastRadius,
			launcher.BlastDamage);

	public static VoidBombProjectile CatalogDefault() =>
		FromLauncher(ChassisWeaponBaselines.VoidBombLauncher());
}
