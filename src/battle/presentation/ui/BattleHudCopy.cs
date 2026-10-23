using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Objectives;
using GrimSpace.Battle.Player;
using GrimSpace.Battle.Units;
using GrimSpace.Application;
using GrimSpace.Math.Grid;
using GrimSpace.Pilots;
using GrimSpace.Units.Enums;
using GrimSpace.Units.Loadouts.Abilities;
using GrimSpace.Units.Specs;

namespace GrimSpace.Battle.Presentation.Ui;

/// <summary>Player-visible battle HUD strings. Centralized for future localization.</summary>
internal static class BattleHudCopy
{
	public const string TurnLabel = "Turn {0}";
	public const string SimulationPhase = "SIMULATION PHASE";
	public const string PauseMenuTitle = "Paused";
	public const string Continue = "Continue";
	public const string Retire = "Retire";
	public const string Restart = "Restart";
	public const string MainMenu = "Main Menu";

	public const string HullTitle = "HP";
	public const string HullTooltip =
		"Hull integrity.\nDamage that gets past the hit face's shields reduces hull.\nAt 0 you're destroyed.";
	public const string ShieldsTitle = "Shields";
	public const string ShieldsTooltip =
		"Each face has its own shield pool.\nThe face toward the attacker absorbs the hit first.";
	public const string FaceShieldAbsorbLine = "Absorbs hits from this direction.";
	public const string FaceShieldPoolLine = "Each face has its own shield pool.";

	public const string MoveTooltip =
		"Move your ship within the movement bubble.";
	public const string ActionPointsTooltip =
		"Moves cost Action Points";
	public const string ManeuverPointsTooltip =
		"Yaw, pitch, and roll maneuvers cost Maneuver Points";

	public const string EndTurn = "Execute Turn";
	public const string EndTurnTooltip = "Execute your queued actions and end your turn.";
	public const string PickFiringMount = "Pick firing mount";
	public const string PickLaunchBay = "Pick launch bay";
	public const string PickVoidBomb = "Pick void bomb";
	public const string ActionUnavailable = "Action is no longer available";

	public const string FocusTooltip = "Refocus camera on your active unit.";
	public const string CycleTargetTooltip = "Cycle targeting reticle.";
	public static string UndoTooltip =>
		$"Undo your last action this turn.";
	public const string BackToPlayer = "Back";
	public const string BackToPlayerTooltip = "Return to your ship and resume planning.";

	public const string ActionLogTitle = "Action Log";
	public const string ActionLogEmpty = "(no actions yet)";

	public const string OutcomeWin = "You Win!";
	public const string OutcomeLose = "You Lose";
	public const string OutcomeDraw = "Draw";
	public const string OutcomeDefault = "Battle Over";
	public const string Reset = "Reset";
	public const string ReturnToStarMap = "Return to Star Map";
	public const string ReturnToMainMenu = "Main Menu";

	public static string Turn(int turnNumber) => string.Format(TurnLabel, turnNumber);

	public static string Charges(int current, int max) => $"{current}/{max}";
	public static string ResourcePoints(string resource, int current, int allotted) =>
		$"{resource} {current}/{allotted}";

	public static string FaceShieldTooltip(string faceName, int current, int max) =>
		$"{faceName} {current}/{max}\n{FaceShieldAbsorbLine}\n{FaceShieldPoolLine}";

	public static string FaceName(ESpatialOrientation face) =>
		face switch
		{
			ESpatialOrientation.Forward => "Forward",
			ESpatialOrientation.Retro => "Aft",
			ESpatialOrientation.Starboard => "Starboard",
			ESpatialOrientation.Port => "Port",
			ESpatialOrientation.Dorsal => "Dorsal",
			ESpatialOrientation.Ventral => "Ventral",
			_ => face.ToString(),
		};

	public static string PickAbilitySource(EPlayerMode mode) =>
		mode switch
		{
			EPlayerMode.SpawnRepurposedMiner => PickLaunchBay,
			EPlayerMode.Detonate => PickVoidBomb,
			_ => PickFiringMount,
		};

	public static string OutcomeTitle(EBattleResult result) =>
		result switch
		{
			EBattleResult.Win => OutcomeWin,
			EBattleResult.Lose => OutcomeLose,
			EBattleResult.Tie => OutcomeDraw,
			_ => OutcomeDefault,
		};

	public static string ScrapDroneSwarmTooltipFor(UnitDisplayState unit) =>
		FirstInstalled<ScrapDroneSwarmSpec>(unit, EAbilityKind.ScrapDroneSwarm) is { } swarm
			? ScrapDroneSwarmTooltipFor(swarm)
			: "Scrap Drone Swarm";

	public static string LightningCannonTooltipFor(UnitDisplayState unit) =>
		FirstInstalled<LightningCannonSpec>(unit, EAbilityKind.LightningCannon) is { } lightningCannon
			? LightningCannonTooltipFor(lightningCannon)
			: "Lightning Cannon";

	public static string VoidBombTooltipFor(UnitDisplayState unit) =>
		FirstInstalled<VoidBombLauncherSpec>(unit, EAbilityKind.VoidBombLauncher) is { } launcher
			? VoidBombTooltipFor(launcher)
			: "Void bomb";

	public static string DetonateTooltipFor(UnitDisplayState unit) =>
		unit.Projectile is { } projectile
			? DetonateTooltipFor(projectile)
			: "Detonate";

	public static string ShieldsUpTooltipFor(UnitDisplayState unit)
	{
		var definition = PilotSkillCatalog.Get(EPilotSkillId.ShieldsUp);
		return
			$"{definition.Name}:\n{definition.Description}\n" +
			$"Next turn: -{ShieldsUpDef.NextTickApPenalty} AP.";
	}

	public static string SpawnRepurposedMinerTooltipFor(UnitDisplayState unit) =>
		FirstInstalled<MinerBaySpec>(unit, EAbilityKind.MinerBay) is { } bay
			? SpawnRepurposedMinerTooltipFor(bay)
			: "Deploy Repurposed Miner";

	public static string ScrapDroneSwarmTooltipFor(ScrapDroneSwarmSpec swarm) =>
		$"Scrap Drone Swarm:\nShort-range {BattleDistance.ApproximateCells(swarm.BurstRange + 1)} homing drones.\n" +
		$"Deals {swarm.Damage} damage.";

	public static string LightningCannonTooltipFor(LightningCannonSpec lightningCannon) =>
		$"Lightning Cannon:\nLong-range {BattleDistance.ApproximateCells(lightningCannon.LineLength + lightningCannon.PyramidRange)} blast along its firing face.\n" +
		$"Deals {lightningCannon.Damage} damage.";

	public static string GoopGunTooltipFor(UnitDisplayState unit) =>
		FirstInstalled<GoopGunSpec>(unit, EAbilityKind.GoopGun) is { } goop
			? GoopGunTooltipFor(goop)
			: "Goop Gun";

	public static string GoopGunTooltipFor(GoopGunSpec goop) =>
		$"Goop Gun:\nCoats an area ahead in blocking goop.\n" +
		$"Placement reach: {BattleDistance.ApproximateCells(goop.Range)}.\n" +
		$"Cooldown: {goop.UnavailableTurns} turns after use.";

	public static string VoidBombTooltipFor(VoidBombLauncherSpec launcher) =>
		$"Void bomb:\nFires a homing bomb for {launcher.BlastDamage} damage, which travels for {launcher.FuelTurns} turns. Beware of hitting yourself!\n" +
		$"Detonation radius: {BattleDistance.ApproximateCells(launcher.BlastRadius)}" +
		$"Cooldown: {launcher.CooldownTurns}.";

	public static string DetonateTooltipFor(VoidBombProjectile projectile) =>
		$"Detonate:\nBlast radius: {BattleDistance.ApproximateCells(projectile.BlastRadius)}; {projectile.BlastDamage} damage.\n" +
		$"Triggers when an enemy is in range, or automatically when fuel runs out.\n" +
		$"Fuel: {projectile.FuelTurns} turns after launch.";

	public static string SpawnRepurposedMinerTooltipFor(MinerBaySpec bay) =>
		$"Deploy Repurposed Miner:\nLaunches a repurposed miner ship from the ventral bay.\n" +
		$"Repurposed Miners can launch scrap drone swarms, and have forward facing shields.\n" +
		$"Max living repurposed miners: {bay.MaxLivingChildren}.\n" +
		$"Cooldown: {bay.CooldownTurns} turns after launch.";

	private static T? FirstInstalled<T>(UnitDisplayState unit, EAbilityKind kind)
		where T : AbilitySpec =>
		unit.Loadout.InstalledAbilities
			.FirstOrDefault(installed => installed.Spec.Kind == kind)
			?.Spec as T;
}
