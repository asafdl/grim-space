using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Effects;
using GrimSpace.Battle.Objectives;
using GrimSpace.Battle.PilotCapabilities;
using GrimSpace.Battle.Player;
using GrimSpace.Battle.Units;
using GrimSpace.Application;
using GrimSpace.Math.Grid;
using GrimSpace.Pilots;
using GrimSpace.Units.Enums;
using GrimSpace.Units.Heat;
using GrimSpace.Units.Loadouts.Abilities;
using GrimSpace.Units.Specs;

namespace GrimSpace.Battle.Presentation.Ui;

internal static class BattleHudCopy
{
	public const string TurnLabel = "Turn {0}";
	public const string SimulationPhase = "SIMULATION PHASE";
	public const string PauseMenuTitle = "Paused";
	public const string Continue = "Continue";
	public const string Retire = "Retire";
	public const string Restart = "Restart";
	public const string MainMenu = "Main Menu";

	public static BattleHudTooltipContent HullTooltip(int current, int max) =>
		new(
			"Hull",
			"Unshielded damage hits hull, at 0 you're destroyed.",
			$"[icon: {Icon.Hull}, text: {current}/{max}]");

	public static BattleHudTooltipContent ShieldsTooltip(
		ESpatialOrientation face,
		int current,
		int max) =>
		new(
			$"{FaceName(face)} Shields",
			"The face toward the attacker absorbs the hit.",
			$"[icon: {Icon.Shield}, text: {current}/{max}]");
	public static BattleHudTooltipContent ActionPointsTooltip =>
		new(
			null,
			"Moves cost Action Points.",
			$"[icon: {Icon.ActionPoints}] [icon: {Icon.Heat}, text: +{HeatChangeEffect.PerApSpent}]");
	public static BattleHudTooltipContent ManeuverPointsTooltip =>
		new(
			null,
			"Yaw, pitch, and roll maneuvers cost Maneuver Points.",
			$"[icon: {Icon.ManeuverPoints}] [icon: {Icon.Heat}, text: +{HeatChangeEffect.PerMpSpent}]");

	public const string EndTurnTooltip = "Execute your queued actions and end your turn.";
	public const string ActionUnavailable = "Action is no longer available";
	public const string PickFiringMount = "Pick firing mount";
	public const string PickLaunchBay = "Pick launch bay";
	public const string PickVoidBomb = "Pick void bomb";

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
	public const string VictoryTitle = "Victory";
	public const string VictoryNoPilotPerformance = "No pilot performance to report.";

	public static string VictoryPilotPerformanceMetadata(
		int damageDealt,
		int damageTaken,
		int healingDone,
		int experience) =>
		$"Damage done {damageDealt} · Damage taken {damageTaken} · Healing done {healingDone}\n"
		+ (experience > 0 ? $"+{experience} XP" : "No XP");

	public static string Turn(int turnNumber) => string.Format(TurnLabel, turnNumber);

	public static string Charges(int current, int max) => $"{current}/{max}";
	public static string ResourcePoints(string resource, int current, int allotted) =>
		$"{resource} {current}/{allotted}";

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

	public static string OutcomeTitle(EBattleResult result) =>
		result switch
		{
			EBattleResult.Win => OutcomeWin,
			EBattleResult.Lose => OutcomeLose,
			EBattleResult.Tie => OutcomeDraw,
			_ => OutcomeDefault,
		};

	public static string PickAbilitySource(EPlayerMode mode) =>
		mode switch
		{
			EPlayerMode.SpawnRepurposedMiner => PickLaunchBay,
			EPlayerMode.Detonate => PickVoidBomb,
			_ => PickFiringMount,
		};

	public static BattleHudTooltipContent ScrapDroneSwarmTooltipFor(UnitDisplayState unit) =>
		FirstInstalled<ScrapDroneSwarmSpec>(unit, EAbilityKind.ScrapDroneSwarm) is { } swarm
			? ScrapDroneSwarmTooltipFor(swarm)
			: new BattleHudTooltipContent("Scrap Drone Swarm", string.Empty);

	public static BattleHudTooltipContent LightningCannonTooltipFor(UnitDisplayState unit) =>
		FirstInstalled<LightningCannonSpec>(unit, EAbilityKind.LightningCannon) is { } lightningCannon
			? LightningCannonTooltipFor(lightningCannon)
			: new BattleHudTooltipContent("Lightning Cannon", string.Empty);

	public static BattleHudTooltipContent VoidBombTooltipFor(UnitDisplayState unit) =>
		FirstInstalled<VoidBombLauncherSpec>(unit, EAbilityKind.VoidBombLauncher) is { } launcher
			? VoidBombTooltipFor(launcher)
			: new BattleHudTooltipContent("Void bomb", string.Empty);

	public static BattleHudTooltipContent DetonateTooltipFor(UnitDisplayState unit) =>
		unit.Projectile is { } projectile
			? DetonateTooltipFor(projectile)
			: new BattleHudTooltipContent("Detonate", string.Empty);

	public static BattleHudTooltipContent ShieldsUpTooltipFor(UnitDisplayState unit)
	{
		var definition = PilotSkillCatalog.Get(EPilotSkillId.ShieldsUp);
		var mechanics =
			$"[icon: {Icon.ActionPoints}, text: -{ShieldsUpDef.NextTickApPenalty} next turn]\n[icon: {Icon.Heat}, text: +{HeatChangeEffect.PerAbilityUse}]";
		var details = string.IsNullOrWhiteSpace(definition.Tooltip.Details)
			? mechanics
			: $"{definition.Tooltip.Details}\n{mechanics}";
		return new BattleHudTooltipContent(
			definition.Tooltip.Title,
			definition.Tooltip.Body,
			details);
	}

	public static BattleHudTooltipContent OverchargeEnginesTooltipFor(UnitDisplayState unit)
	{
		var definition = PilotSkillCatalog.Get(EPilotSkillId.OverchargeEngines);
		var mechanics =
			$"[icon: {Icon.ActionPoints}, text: Refresh up to {PilotSkillPercents.OverchargeEngines.RefreshedAp}]\n[icon: {Icon.Cooldown}, text: {PilotSkillPercents.OverchargeEngines.CooldownTurns} turns]\n[icon: {Icon.ActionPoints}, text: -{PilotSkillPercents.OverchargeEngines.NextTurnApPenalty} next turn]";
		var details = string.IsNullOrWhiteSpace(definition.Tooltip.Details)
			? mechanics
			: $"{definition.Tooltip.Details}\n{mechanics}";
		return new BattleHudTooltipContent(
			definition.Tooltip.Title,
			definition.Tooltip.Body,
			details);
	}

	public static BattleHudTooltipContent BraceTooltipFor(UnitDisplayState unit)
	{
		var definition = PilotSkillCatalog.Get(EPilotSkillId.Brace);
		var mechanics =
			$"[icon: {Icon.Heat}, text: +{HeatChangeEffect.PerAbilityUse}]\n[icon: {Icon.Cooldown}, text: 1 per turn]";
		var details = string.IsNullOrWhiteSpace(definition.Tooltip.Details)
			? mechanics
			: $"{definition.Tooltip.Details}\n{mechanics}";
		return new BattleHudTooltipContent(
			definition.Tooltip.Title,
			definition.Tooltip.Body,
			details);
	}

	public static BattleHudTooltipContent SpawnRepurposedMinerTooltipFor(UnitDisplayState unit) =>
		FirstInstalled<MinerBaySpec>(unit, EAbilityKind.MinerBay) is { } bay
			? SpawnRepurposedMinerTooltipFor(bay)
			: new BattleHudTooltipContent("Deploy Repurposed Miner", string.Empty);

	public static BattleHudTooltipContent HeatTooltip(
		HeatSpec profile)
	{
		return new(
			"Heat",
			$"When reaching max [icon: {Icon.Heat}] enter [icon: {Icon.Overdrive}] for this turn then [icon: {Icon.Overheat}] next turn.",
			$"[icon: {Icon.Heat}, text: -{profile.ClearPerTurn} / turn ]\n[icon: {Icon.Overdrive}][icon: {Icon.Damage}, text: +{OverdriveDamageBonusModifier.BonusPercent}%]\n[icon: {Icon.Overheat}, text: All abilities go on][icon: {Icon.Cooldown}]");
	}

	public static BattleHudTooltipContent ScrapDroneSwarmTooltipFor(ScrapDroneSwarmSpec swarm) =>
		new(
			"Scrap Drone Swarm",
			$"Short-range {BattleDistance.ApproximateCells(swarm.BurstRange + 1)} homing drones. Damages enemies and heals allies in the burst.",
			$"[icon: {Icon.Damage}, text: {swarm.Damage}] [icon: {Icon.Heal}, text: {swarm.Heal}]\n[icon: {Icon.Heat}, text: +{HeatChangeEffect.PerAbilityUse}]");

	public static BattleHudTooltipContent LightningCannonTooltipFor(LightningCannonSpec lightningCannon) =>
		new(
			"Lightning Cannon",
			$"{BattleDistance.ApproximateCells(lightningCannon.LineLength)} beam. Chains up to {lightningCannon.MaxChains} times within {BattleDistance.ApproximateCells(lightningCannon.ChainRange)}.\n" +
			$"-{lightningCannon.ChainFalloffPercentPerHop}% per chain hop.",
			$"[icon: {Icon.Damage}, text: {lightningCannon.Damage}]\n[icon: {Icon.Heat}, text: +{HeatChangeEffect.PerAbilityUse}]");

	public static BattleHudTooltipContent GoopGunTooltipFor(UnitDisplayState unit) =>
		FirstInstalled<GoopGunSpec>(unit, EAbilityKind.GoopGun) is { } goop
			? GoopGunTooltipFor(goop)
			: new BattleHudTooltipContent("Goop Gun", string.Empty);

	public static BattleHudTooltipContent GoopGunTooltipFor(GoopGunSpec goop) =>
		new(
			"Goop Gun",
			$"Coats an area in blocking goop.",
			$"[icon: {Icon.Cooldown}, text: {goop.UnavailableTurns} turns]\n[icon: {Icon.Heat}, text: +{HeatChangeEffect.PerAbilityUse}]");

	public static BattleHudTooltipContent VoidBombTooltipFor(VoidBombLauncherSpec launcher) =>
		new(
			"Void bomb",
			$"Fires a homing bomb that travels for {launcher.FuelTurns} turns. Beware of hitting yourself!\n" +
			$"Detonation radius: {BattleDistance.ApproximateCells(launcher.BlastRadius)}.",
			$"[icon: {Icon.Damage}, text: {launcher.BlastDamage}]\n[icon: {Icon.Cooldown}, text: {launcher.CooldownTurns} turns]\n[icon: {Icon.Heat}, text: +{HeatChangeEffect.PerAbilityUse}]");

	public static BattleHudTooltipContent DetonateTooltipFor(VoidBombProjectile projectile) =>
		new(
			"Detonate",
			$"Blast radius: {BattleDistance.ApproximateCells(projectile.BlastRadius)}.\n" +
			$"Triggers when an enemy is in range, or automatically when fuel runs out.\n" +
			$"Fuel: {projectile.FuelTurns} turns after launch.",
			$"[icon: {Icon.Damage}, text: {projectile.BlastDamage}]");

	public static BattleHudTooltipContent SpawnRepurposedMinerTooltipFor(MinerBaySpec bay) =>
		new(
			"Deploy Repurposed Miner",
			$"Launches a repurposed miner ship from the ventral bay.\n" +
			$"Max living repurposed miners: {bay.MaxLivingChildren}.",
			$"[icon: {Icon.Cooldown}, text: {bay.CooldownTurns} turns]\n[icon: {Icon.Heat}, text: +{HeatChangeEffect.PerAbilityUse}]");

	private static T? FirstInstalled<T>(UnitDisplayState unit, EAbilityKind kind)
		where T : AbilitySpec =>
		unit.Loadout.InstalledAbilities
			.FirstOrDefault(installed => installed.Spec.Kind == kind)
			?.Spec as T;
}
