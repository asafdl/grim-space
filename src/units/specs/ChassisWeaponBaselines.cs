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
	internal const int RepurposedMinerCooldownTurns = 2;
	internal const int MaxLivingRepurposedMinerChildren = 5;
	internal const int VoidBombLauncherCooldownTurns = 3;
	internal const int StarterVoidBombFuelTurns = 2;

	internal static ScrapDroneSwarmSpec ScrapDroneSwarm() => new(ScrapDroneSwarmsPerTurn, ScrapDroneSwarmDamage, ScrapDroneSwarmRange);

	internal static LightningCannonSpec LightningCannon() =>
		new(LightningCannonsPerTurn, LightningCannonDamage, LightningCannonLineLength, LightningCannonPyramidRange);

	internal static LightningCannonSpec StarterLightningCannon() =>
		new(LightningCannonsPerTurn, StarterLightningCannonDamage, StarterLightningCannonLineLength, LightningCannonPyramidRange);

	internal static VoidBombLauncherSpec StarterVoidBombLauncher() =>
		VoidBombLauncher(StarterVoidBombFuelTurns);

	internal static MinerBaySpec MinerBay(ShipSpec repurposedMinerChild) =>
		new(RepurposedMinerCooldownTurns, repurposedMinerChild, MaxLivingRepurposedMinerChildren);

	internal static VoidBombLauncherSpec VoidBombLauncher(int fuelTurns = VoidBombSpec.FuelTurns) =>
		new(
			VoidBombLauncherCooldownTurns,
			fuelTurns,
			VoidBombSpec.MovementActionPoints,
			VoidBombSpec.ForwardMoveApCost,
			VoidBombSpec.LateralMoveApCost,
			VoidBombSpec.BlastRadius,
			VoidBombSpec.BlastDamage);
}
