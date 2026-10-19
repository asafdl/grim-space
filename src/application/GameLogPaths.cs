using Godot;

namespace GrimSpace.Application;

public static class GameLogPaths
{
	public const string GodotLogRelativePath = "logs/godot.log";

	public static string GodotLogFilePath() =>
		Path.Combine(OS.GetUserDataDir(), GodotLogRelativePath);
}
