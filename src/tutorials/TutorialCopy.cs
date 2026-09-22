using GrimSpace.Components;

namespace GrimSpace.Tutorials;

/// <summary>Player-visible tutorial strings. Input bindings come from <see cref="InputShortcutText"/>.</summary>
internal static class TutorialCopy
{
	public static string EndTurnPrompt =>
		$"Press {InputShortcutText.Space} to end your turn or click the HUD.";

	public const string MoveToGhostShip = "Pilot, lets go get the enemies, move your ship forward, to the marked location.";

	public static string Turn2MovePrompt =>
		$"Enemies closing in, lets get in position to shoot — You can hold {InputShortcutText.RightMouse} and drag to orbit camera. " +
		$"Hold {InputShortcutText.LeftMouse} to set your heading and {InputShortcutText.ScrollWheel} to roll your ship, align it to ghost. ";

	public const string LaunchVentralTorpedo =
		"Weapons have launch mounts, select torpedo from HUD and shoot from the highlighted underside mount.";

	public const string MoveToMarkedGhostAssistance = "Move to the marked ghost ship.";

	public const string MatchGhostPoseAssistance =
		"Match the ghost: pitch heading to dorsal, then roll so the underside faces the enemy.";

	public const string QueueVentralTorpedoAssistance =
		"Queue a torpedo from the underside (ventral) mount.";

	public const string UndoAndRetryAssistance = "Undo and try again";

	public const string TutorialGraduationMessage =
		"Tutorial completed — explore the world, earn credits, and enjoy. " +
		"You can turn tutorials back on any time in settings.";

	public static string MapMoveToPoi(string poiId, string poiLabel) =>
		$"{InputShortcutText.LeftClick} the [url={poiId}]{poiLabel}[/url] on the map to move your fleet there.";
}
