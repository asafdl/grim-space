using GrimSpace.Application;

namespace GrimSpace.Tutorials;

/// <summary>Player-visible tutorial strings. Hotkeys use <see cref="GameInputBindings.Label"/>.</summary>
internal static class TutorialCopy
{
	public static string ShieldApproach =>
		$"You are in combat simulation mode, here you can plan out your actions. You can undo anything.\n" +
		$"Move to the marked location {GameInputBindings.Label("battle_primary_click")} and match its heading to face the enemy, an icon should appear to indicate that you have a shot.";

	public static string CameraControls =>
		$"3 Dimensional space is hard to navigate, make full use of the camera controls:\n- HOLD {GameInputBindings.Label("battle_camera_orbit")} and drag to orbit.\n" +
		$"- Move camera with {GameInputBindings.Label("battle_pan_up")} {GameInputBindings.Label("battle_pan_left")} " +
		$"{GameInputBindings.Label("battle_pan_down")} {GameInputBindings.Label("battle_pan_right")}, " +
		$"{GameInputBindings.Label("battle_pan_vertical_up")} / {GameInputBindings.Label("battle_pan_vertical_down")} for height, " +
		$"- Press {GameInputBindings.Label("battle_focus")} to refocus on your ship.";

	public static string QueueShieldShot =>
		$"Select the highlighted ability in the ability bar or use hotkey ({GameInputBindings.Label("battle_ability_1")}) to queue a shot at the enemy ship.\n" +
		"In simulation mode, you can see the damage preview of every shot you take.";

	public static string WeaponsAndMovement => "All actions generate Heat.\nWeapons cost nothing but have a cooldown.\nMovement costs Action Points.\nChanging direction and rolling costs Maneuver Points.";

	public static string UndoShieldShot =>
		"The miner's front shield absorbs the damage, you can break shields but it's less efficient than finding an exposed side.\n" +
		$"Press Undo ({GameInputBindings.Label("battle_undo")}).";

	public static string FindKillShot =>
		$"Find your own firing position and queue a killing shot with the Lightning Cannon.\nYou can hold {GameInputBindings.Label("battle_isolate_movement")} while hovering a cell to see exactly where the move ends.\n" +
		CameraControls;

	public static string ExecuteKillShot =>
		$"The preview predicts a kill. Press Execute ({GameInputBindings.Label("battle_end_turn")}) to commit your plan.";

	public const string MoveToMarkedGhostAssistance = "Move to the marked location.";

	public const string QueueShieldShotAssistance =
		"Match the marked pose and queue the lightining cannon to hit the enemy ship's front shield.";

	public const string QueueKillShotAssistance =
		"Queue a killing shot against the nearby ship, any unshielded side will do.";

	public static string UndoAndRetryAssistance =>
		$"Press {GameInputBindings.Label("battle_undo")} to undo and try again.";

	public const string EndBattleDialog =
		"Target destroyed. These are the basics of combat, use them to your advantage. " +
		"Look for unshielded directions, preview your attacks, and Execute when you're ready.";

	public const string TutorialGraduationMessage =
		"Tutorial completed — explore the world, earn resources, and don't forget to upgrade your ship and heal! " +
		"You can redo tutorial by reactivating it in settings and starting a new game.";

}
