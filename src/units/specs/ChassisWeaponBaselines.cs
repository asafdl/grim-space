using GrimSpace.Math.Grid;
using GrimSpace.Units.Loadouts.Abilities;

namespace GrimSpace.Units.Specs;

internal static class ChassisWeaponBaselines
{
	internal const int FlakDamage = 1;
	internal const int FlakRange = 2;
	internal const int FlaksPerTurn = 1;
	internal const int RailgunDamage = 3;
	internal const int RailgunLineLength = 8;
	internal const int RailgunPyramidRange = 2;
	internal const int RailgunsPerTurn = 1;
	internal const int StarterRailgunDamage = 2;
	internal const int StarterRailgunLineLength = 5;
	internal const int PatrolCooldownTurns = 2;
	internal const int MaxLivingPatrolChildren = 5;
	internal const int TorpedoLauncherCooldownTurns = 3;
	internal const int StarterTorpedoFuelTurns = 2;

	internal static FlakSpec Flak() => new(FlaksPerTurn, FlakDamage, FlakRange);

	internal static RailgunSpec Railgun() =>
		new(RailgunsPerTurn, RailgunDamage, RailgunLineLength, RailgunPyramidRange);

	internal static RailgunSpec StarterRailgun() =>
		new(RailgunsPerTurn, StarterRailgunDamage, StarterRailgunLineLength, RailgunPyramidRange);

	internal static TorpedoLauncherSpec StarterTorpedoLauncher() =>
		TorpedoLauncher(StarterTorpedoFuelTurns);

	internal static PatrolBaySpec PatrolBay(ShipSpec patrolChild) =>
		new(PatrolCooldownTurns, patrolChild, MaxLivingPatrolChildren);

	internal static TorpedoLauncherSpec TorpedoLauncher(int fuelTurns = TorpedoSpec.FuelTurns) =>
		new(
			TorpedoLauncherCooldownTurns,
			fuelTurns,
			TorpedoSpec.MovementActionPoints,
			TorpedoSpec.ForwardMoveApCost,
			TorpedoSpec.LateralMoveApCost,
			TorpedoSpec.BlastRadius,
			TorpedoSpec.BlastDamage);
}
