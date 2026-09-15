namespace GrimSpace.Components;

public static class InputShortcutText
{
	public static string WithPrimaryModifier(string key) =>
		$"{PrimaryModifierFor(OperatingSystem.IsMacOS())}+{key}";

	internal static string PrimaryModifierFor(bool isMacOs) =>
		isMacOs ? "Cmd" : "Ctrl";
}
