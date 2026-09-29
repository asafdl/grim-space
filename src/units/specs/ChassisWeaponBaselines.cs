using GrimSpace.Math.Grid;
using GrimSpace.Units.Loadouts.Abilities;

namespace GrimSpace.Units.Specs;

internal static class ChassisWeaponBaselines
{
	internal const int ScrapDroneSwarmDamage = 1;
	internal const int ScrapDroneSwarmRange = 2;
	internal const int ScrapDroneSwarmsPerTurn = 1;
	internal const int LightningCannonDamage = 3;
	internal const int LightningCannonLineLength = 8;
	internal const int LightningCannonPyramidRange = 2;
	internal const int LightningCannonsPerTurn = 1;
	internal const int StarterLightningCannonDamage = 2;
	internal const int StarterLightningCannonLineLength = 5;
	internal const int PatrolCooldownTurns = 2;
	internal const int MaxLivingPatrolChildren = 5;
	internal const int TorpedoLauncherCooldownTurns = 3;
	internal const int StarterTorpedoFuelTurns = 2;

	internal static ScrapDroneSwarmSpec ScrapDroneSwarm() => new(ScrapDroneSwarmsPerTurn, ScrapDroneSwarmDamage, ScrapDroneSwarmRange);

	internal static LightningCannonSpec LightningCannon() =>
		new(LightningCannonsPerTurn, LightningCannonDamage, LightningCannonLineLength, LightningCannonPyramidRange);

	internal static LightningCannonSpec StarterLightningCannon() =>
		new(LightningCannonsPerTurn, StarterLightningCannonDamage, StarterLightningCannonLineLength, LightningCannonPyramidRange);

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
