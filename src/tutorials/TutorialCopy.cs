using GrimSpace.Application;

namespace GrimSpace.Tutorials;

/// <summary>Player-visible tutorial strings. Hotkeys use <see cref="GameInputBindings.Label"/>.</summary>
internal static class TutorialCopy
{
	public static string EndTurnPrompt =>
		$"You are currently in Simulation Phase, nothing is committed until you press {GameInputBindings.Label("battle_end_turn")} to execute.\nYou can freely undo ({GameInputBindings.Label("battle_undo")}) your actions.";

	public static string MoveToGhostShip =>
		$"Pilot, lets go get the enemies — You have 4 Action points per turn, advance to the marked location use {GameInputBindings.Label("battle_primary_click")}, adjust heading to match.";

	public static string CameraControls =>
		$"3 Dimensional space is hard to navigate, make full use of the camera controls:\n- HOLD {GameInputBindings.Label("battle_camera_orbit")} and drag to orbit.\n" +
		$"- Move camera with {GameInputBindings.Label("battle_pan_up")} {GameInputBindings.Label("battle_pan_left")} " +
		$"{GameInputBindings.Label("battle_pan_down")} {GameInputBindings.Label("battle_pan_right")}, " +
		$"{GameInputBindings.Label("battle_pan_vertical_up")} / {GameInputBindings.Label("battle_pan_vertical_down")} for height, " +
		$"or HOLD {GameInputBindings.Label("battle_camera_pan")} and drag.\n" +
		$"- Press {GameInputBindings.Label("battle_focus")} to refocus on your ship.";

	public static string Turn2MovePrompt =>
		$"Enemies closing in, using weapons forces you to reposition your ship, head to the marked location and align your ship.\n" +
		$"Click {GameInputBindings.Label("battle_primary_click")} then {GameInputBindings.Label("battle_roll_clockwise")} / " +
		$"{GameInputBindings.Label("battle_roll_counterclockwise")} to roll, then click {GameInputBindings.Label("battle_primary_click")} to match heading.";

	public static string LaunchVentralVoidBomb =>
		$"Weapons have launch mounts, select void bomb from Ability Bar or use hotkey {GameInputBindings.Label("battle_ability_2")}.\n" +
		$"Some weapons will shoot instantly, others, like void bombs, will launch a homing projectile at enemy.\nHover over the ability icon for more information.";

	public const string MoveToMarkedGhostAssistance = "Move to the marked location.";

	public static string MatchGhostPoseAssistance =>
		$"Match the preview: click {GameInputBindings.Label("battle_primary_click")}, then roll ({GameInputBindings.Label("battle_roll_clockwise")} / " +
		$"{GameInputBindings.Label("battle_roll_counterclockwise")}) and match heading.";

	public const string QueueVentralVoidBombAssistance =
		"Queue a void bomb.";

	public static string UndoAndRetryAssistance =>
		$"Press {GameInputBindings.Label("battle_undo")} to undo and try again.";

	public const string EndBattleDialog = "Take full advantage of camera controls and firing hints. Now go kill those defragging rust boxes.";

	public const string TutorialGraduationMessage =
		"Tutorial completed — explore the world, earn resources, and don't forget to upgrade your ship and heal at the Trade Hub! " +
		"You can turn tutorials back on any time in settings.";

}
