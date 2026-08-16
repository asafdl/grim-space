using GrimSpace.Battle.Abilities;
using GrimSpace.Battle.Objectives;
using GrimSpace.Math.Grid;
using GrimSpace.Run;

namespace GrimSpace.Battle.Presentation.Ui;

/// <summary>Player-visible battle HUD strings. Centralized for future localization.</summary>
internal static class BattleHudCopy
{
	public const string TurnLabel = "Turn {0}";
	public const string PauseMenuTitle = "Paused";
	public const string Continue = "Continue";
	public const string ContinueTooltip = "Resume battle";
	public const string Retire = "Retire";
	public const string RetireTooltip = "Forfeit — you lose";
	public const string Restart = "Restart";
	public const string RestartTooltip = "Restart encounter with a new random layout";
	public const string MainMenu = "Main Menu";
	public const string MainMenuTooltip = "Leave battle and return to the title screen";

	public const string HullTitle = "HP";
	public const string HullTooltip =
		"Hull integrity.\nDamage that gets past the hit face's shields reduces hull.\nAt 0 you're destroyed.";
	public const string ShieldsTitle = "Shields";
	public const string ShieldsTooltip =
		"Each face has its own shield pool.\nThe face toward the attacker absorbs the hit first.";
	public const string FaceShieldAbsorbLine = "Absorbs hits from this direction.";
	public const string FaceShieldPoolLine = "Each face has its own shield pool.";

	public const string MomentumTooltip =
		"Momentum:\nForward speed carried from movement (0–2).\n" +
		"Higher levels grant free forward steps but make lateral drift and braking cost more AP.\n" +
		"Build it by moving forward; lose it by braking or ending your turn without moving.";
	public const string MoveTooltip =
		"Move:\nSpend AP to path across the grid.\nPositioning decides weapon arcs and which shield face takes a hit.";
	public const string YawTooltip = "Yaw:\nTurn your heading. Costs 1 AP.";
	public const string SpinTooltip = "Spin:\nRoll the ship. Costs 1 AP.";

	public static string FlakTooltip =>
		$"Flak:\nSide burst (port or starboard).\n" +
		$"Range: {CombatConfig.MaxFlakManhattanRange} cells.\n" +
		$"Deals {CombatConfig.FlakDamage} damage and strips momentum.\n" +
		$"Cooldown: {CombatConfig.FlaksPerTurn} use per turn.";

	public static string RailgunTooltip =>
		$"Railgun:\nFires in a long straight line ahead.\n" +
		$"Range: {CombatConfig.MaxRailgunManhattanRange} cells.\n" +
		$"Deals {CombatConfig.RailgunDamage} damage.\n" +
		$"Cooldown: {CombatConfig.RailgunsPerTurn} use per turn.";

	public static string TorpedoTooltip =>
		$"Torpedo:\nFires in a set direction.\n" +
		$"Chases enemies in path for {TorpedoConfig.Fuel} turns.\n" +
		$"Blast radius: {TorpedoConfig.BlastRadius} cells, {TorpedoConfig.BlastDamage} damage.\n" +
		$"Cooldown: {TorpedoConfig.CooldownTurns} turns after launch.";

	public static string DetonateTooltip =>
		$"Detonate:\nExplodes for {TorpedoConfig.BlastDamage} damage in a {TorpedoConfig.BlastRadius}-cell radius.\n" +
		$"Triggers when an enemy is in range, or automatically when fuel runs out.\n" +
		$"Fuel: {TorpedoConfig.Fuel} turns after launch.";

	public static string SpawnPatrolTooltip =>
		$"Deploy Patrol:\nLaunches a patrol ship from the ventral bay.\n" +
		$"Patrols can shoot flak cannons, and have forward facing shields.\n" +
		$"Max living patrols: {CombatConfig.MaxLivingPatrolChildren}.\n" +
		$"Cooldown: {CombatConfig.PatrolCooldownTurns} turns after launch.";
	public const string EndTurn = "End Turn";
	public const string EndTurnTooltip = "End your turn and resolve the round.\nAP and cooldowns refresh.";

	public const string FocusTooltip = "Snap the camera to your active ship.";
	public const string UndoTooltip = "Undo your last action this turn.\n(Ctrl/Cmd+Z)";
	public const string BackToPlayer = "Back";
	public const string BackToPlayerTooltip = "Return to your ship and resume planning.";

	public const string ActionLogTitle = "Action Log";
	public const string ActionLogEmpty = "(no actions yet)";

	public const string OutcomeWin = "You Win!";
	public const string OutcomeLose = "You Lose";
	public const string OutcomeDraw = "Draw";
	public const string OutcomeDefault = "Battle Over";
	public const string Reset = "Reset";

	public const string IntroTitle = "Engage";
	public const string ObjectiveEliminateOpponents = "Objective: Eliminate all opponents";

	public static string Turn(int turnNumber) => string.Format(TurnLabel, turnNumber);

	public static string Charges(int current, int max) => $"{current}/{max}";

	public static string MomentumStat(int current, int max) => $"M{current}/{max}";

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

	public static string OutcomeTitle(EBattleResult result) =>
		result switch
		{
			EBattleResult.Win => OutcomeWin,
			EBattleResult.Lose => OutcomeLose,
			EBattleResult.Tie => OutcomeDraw,
			_ => OutcomeDefault,
		};

	public static string ObjectiveLabel(EObjective objective) =>
		objective switch
		{
			EObjective.EliminateOpponents => ObjectiveEliminateOpponents,
			_ => objective.ToString(),
		};
}
