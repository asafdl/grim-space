using GrimSpace.Battle.Objectives;
using GrimSpace.Battle.Player;
using GrimSpace.Battle.Units;
using GrimSpace.Components;
using GrimSpace.Math.Grid;
using GrimSpace.Units.Enums;
using GrimSpace.Units.Loadouts.Abilities;
using GrimSpace.Units.Specs;

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

	public const string MoveTooltip =
		"Move:\nEach AP advances one cell and may include one quarter-turn and one quarter-roll.";

	public const string EndTurn = "End Turn";
	public const string EndTurnTooltip = "End your turn and resolve the round.\nAP and cooldowns refresh.";

	public const string PickFiringMount = "Pick firing mount";
	public const string PickLaunchBay = "Pick launch bay";
	public const string PickTorpedo = "Pick torpedo";
	public const string ActionUnavailable = "Action is no longer available";

	public const string FocusTooltip = "Snap the camera to your active ship.";
	public static readonly string UndoTooltip =
		$"Undo your last action this turn.\n({InputShortcutText.WithPrimaryModifier("Z")})";
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

	public static string Turn(int turnNumber) => string.Format(TurnLabel, turnNumber);

	public static string Charges(int current, int max) => $"{current}/{max}";

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
			EPlayerMode.SpawnPatrol => PickLaunchBay,
			EPlayerMode.Detonate => PickTorpedo,
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

	public static string FlakTooltipFor(UnitDisplayState unit) =>
		FirstInstalled<FlakSpec>(unit, EAbilityKind.Flak) is { } flak
			? FlakTooltipFor(flak)
			: "Flak";

	public static string RailgunTooltipFor(UnitDisplayState unit) =>
		FirstInstalled<RailgunSpec>(unit, EAbilityKind.Railgun) is { } railgun
			? RailgunTooltipFor(railgun)
			: "Railgun";

	public static string TorpedoTooltipFor(UnitDisplayState unit) =>
		FirstInstalled<TorpedoLauncherSpec>(unit, EAbilityKind.TorpedoLauncher) is { } launcher
			? TorpedoTooltipFor(launcher)
			: "Torpedo";

	public static string DetonateTooltipFor(UnitDisplayState unit) =>
		unit.Projectile is { } projectile
			? DetonateTooltipFor(projectile)
			: "Detonate";

	public static string SpawnPatrolTooltipFor(UnitDisplayState unit) =>
		FirstInstalled<PatrolBaySpec>(unit, EAbilityKind.PatrolBay) is { } bay
			? SpawnPatrolTooltipFor(bay)
			: "Deploy Patrol";

	public static string FlakTooltipFor(FlakSpec flak) =>
		$"Flak:\nSide burst (port or starboard).\n" +
		$"Range: {AbilityReach.MaxManhattanFromFirer(flak)} cells.\n" +
		$"Deals {flak.Damage} damage.\n" +
		$"Cooldown: {flak.UsesPerTurn} use per turn.";

	public static string RailgunTooltipFor(RailgunSpec railgun) =>
		$"Railgun:\nFires in a long straight line ahead.\n" +
		$"Range: {AbilityReach.MaxManhattanFromFirer(railgun)} cells.\n" +
		$"Deals {railgun.Damage} damage.\n" +
		$"Cooldown: {railgun.UsesPerTurn} use per turn.";

	public static string TorpedoTooltipFor(TorpedoLauncherSpec launcher) =>
		$"Torpedo:\nFires in a set direction.\n" +
		$"Travels for {launcher.FuelTurns} turns with {launcher.MovementActionPoints} AP per turn.\n" +
		$"Forward movement costs {launcher.ForwardMoveApCost} AP; lateral movement costs {launcher.LateralMoveApCost} AP.\n" +
		$"Blast radius: {launcher.BlastRadius} cells, {launcher.BlastDamage} damage.\n" +
		$"Cooldown: {launcher.CooldownTurns} turns after launch.";

	public static string DetonateTooltipFor(TorpedoProjectile projectile) =>
		$"Detonate:\nExplodes for {projectile.BlastDamage} damage in a {projectile.BlastRadius}-cell radius.\n" +
		$"Triggers when an enemy is in range, or automatically when fuel runs out.\n" +
		$"Fuel: {projectile.FuelTurns} turns after launch.";

	public static string SpawnPatrolTooltipFor(PatrolBaySpec bay) =>
		$"Deploy Patrol:\nLaunches a patrol ship from the ventral bay.\n" +
		$"Patrols can shoot flak cannons, and have forward facing shields.\n" +
		$"Max living patrols: {bay.MaxLivingChildren}.\n" +
		$"Cooldown: {bay.CooldownTurns} turns after launch.";

	private static T? FirstInstalled<T>(UnitDisplayState unit, EAbilityKind kind)
		where T : AbilitySpec =>
		unit.Loadout.InstalledAbilities
			.FirstOrDefault(installed => installed.Spec.Kind == kind)
			?.Spec as T;
}
