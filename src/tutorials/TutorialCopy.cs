using GrimSpace.Application;

namespace GrimSpace.Tutorials;

/// <summary>Player-visible tutorial strings. Hotkeys use <see cref="GameInputBindings.Label"/>.</summary>
internal static class TutorialCopy
{
	public static string EndTurnPrompt =>
		$"Press {GameInputBindings.Label("battle_end_turn")} to end your turn or click the Ability Bar on bottom of screen.";

	public static string MoveToGhostShip =>
		$"Pilot, lets go get the enemies — advance to the marked location use {GameInputBindings.Label("battle_primary_click")}.";

	public static string CameraControls =>
		$"3 Dimensional space is hard to navigate, make full use of the camera controls:\n- HOLD {GameInputBindings.Label("battle_camera_orbit")} and drag to orbit.\n" +
		$"- Move camera with {GameInputBindings.Label("battle_pan_up")} {GameInputBindings.Label("battle_pan_left")} " +
		$"{GameInputBindings.Label("battle_pan_down")} {GameInputBindings.Label("battle_pan_right")}, " +
		$"or HOLD {GameInputBindings.Label("battle_camera_pan")} and drag.\n" +
		$"- Press {GameInputBindings.Label("battle_focus")} to refocus on your ship.";

	public static string Turn2MovePrompt =>
		$"Enemies closing in, using weapons forces you to reposition your ship. Lets get in position to shoot — Keep HOLDING {GameInputBindings.Label("battle_primary_click")} " +
		$"and DRAG toward the green heading arrow.\nKeep HOLDING and {GameInputBindings.Label("battle_roll_clockwise")} / " +
		$"{GameInputBindings.Label("battle_roll_counterclockwise")} to roll. The gold section of the preview hull is the topside (dorsal); " +
		$"put the opposite underside (ventral) toward the enemy.\nAlign yourself to the marked ship.";

	public static string LaunchVentralVoidBomb =>
		$"Weapons have launch mounts, select void bomb from Ability Bar and click the highlighted underside (ventral) mount—the side opposite the gold topside shown in the movement preview.\n" +
		$"Some weapons will shoot instantly at end turn, others, like void bombs, will launch a homing projectile at enemy.\nHover Ability to see more information.";

	public const string MoveToMarkedGhostAssistance = "Move to the marked location.";

	public static string MatchGhostPoseAssistance =>
		$"Match the preview: drag heading to the green arrow, then roll ({GameInputBindings.Label("battle_roll_clockwise")} / " +
		$"{GameInputBindings.Label("battle_roll_counterclockwise")}). Gold marks the topside (dorsal); put the opposite underside (ventral) toward the enemy.";

	public const string QueueVentralVoidBombAssistance =
		"Queue a void bomb from the underside (ventral) mount, opposite the gold topside shown during movement.";

	public static string UndoAndRetryAssistance =>
		$"Press {GameInputBindings.Label("battle_undo")} to undo and try again.";

	public const string EndBattleDialog = "Take full advantage of camera controls and firing hints. Now go kill those defragging rust boxes.";

	public const string TutorialGraduationMessage =
		"Tutorial completed — explore the world, earn resources, and don't forget to upgrade your ship and heal at the Trade Hub! " +
		"You can turn tutorials back on any time in settings.";

}
