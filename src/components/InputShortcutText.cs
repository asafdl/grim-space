namespace GrimSpace.Components;

public static class InputShortcutText
{
	public const string Space = "Space";
	public const string LeftMouse = "LMB";
	public const string RightMouse = "RMB";
	public const string ScrollWheel = "scroll";
	public const string LeftClick = "Left-click";

	public static string WithPrimaryModifier(string key) =>
		$"{PrimaryModifierFor(OperatingSystem.IsMacOS())}+{key}";

	internal static string PrimaryModifierFor(bool isMacOs) =>
		isMacOs ? "Cmd" : "Ctrl";
}
