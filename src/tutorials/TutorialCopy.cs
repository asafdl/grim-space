using GrimSpace.Application;

namespace GrimSpace.Tutorials;

/// <summary>Player-visible tutorial strings. Hotkeys use <see cref="GameInputBindings.Label"/>.</summary>
internal static class TutorialCopy
{
	public static string EndTurnPrompt =>
		$"Press {GameInputBindings.Label("battle_end_turn")} to end your turn or click the Ability Bar on bottom of screen.";

	public static string MoveToGhostShip =>
		$"Pilot, lets go get the enemies — press {GameInputBindings.Label("battle_move_mode")} for move mode, then advance to the marked location.";

	public static string Turn2MovePrompt =>
		$"Enemies closing in, lets get in position to shoot — hold {GameInputBindings.Label("battle_camera_orbit")} and drag to orbit the camera, " +
		$"or pan with {GameInputBindings.Label("battle_pan_up")} {GameInputBindings.Label("battle_pan_left")} " +
		$"{GameInputBindings.Label("battle_pan_down")} {GameInputBindings.Label("battle_pan_right")}. " +
		$"Hold {GameInputBindings.Label("battle_primary_click")} to set heading; use {GameInputBindings.Label("battle_roll_clockwise")} / " +
		$"{GameInputBindings.Label("battle_roll_counterclockwise")} to roll. Align to the ghost.";

	public const string LaunchVentralTorpedo =
		"Weapons have launch mounts, select torpedo from Ability Bar and shoot from the highlighted underside mount.";

	public const string MoveToMarkedGhostAssistance = "Move to the marked location.";

	public static string MatchGhostPoseAssistance =>
		$"Match the preview: pitch heading to UP (dorsal), then roll ({GameInputBindings.Label("battle_roll_clockwise")} / " +
		$"{GameInputBindings.Label("battle_roll_counterclockwise")}) so the underside faces the enemy.";

	public const string QueueVentralTorpedoAssistance =
		"Queue a torpedo from the underside (ventral) mount.";

	public static string UndoAndRetryAssistance =>
		$"Press {GameInputBindings.Label("battle_undo")} to undo and try again.";

	public const string EndBattleDialog = "3 Dimensional space is hard to navigate, take full advantage of camera controls and firing hints. Now go kill those scum.";

	public const string TutorialGraduationMessage =
		"Tutorial completed — explore the world, earn resources, and don't forget to upgrade your ship in the Trade Hub! " +
		"You can turn tutorials back on any time in settings.";

}
