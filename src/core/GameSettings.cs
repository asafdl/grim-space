using Godot;

namespace GrimSpace.Core;

public static class GameSettings
{
	private const string SettingsPath = "user://settings.cfg";

	public static readonly Vector2I[] SupportedResolutions =
	[
		new(2560, 1440),
		new(1920, 1080),
		new(1600, 900),
		new(1280, 720),
	];

	public static (string Mode, int Width, int Height) ReadVideoConfig()
	{
		var hasSettings = TryLoad(out var config);

		var mode = hasSettings
			? config!.GetValue("video", "mode", "fullscreen").AsString()
			: "fullscreen";
		var width = hasSettings ? config!.GetValue("video", "width", 0).AsInt32() : 0;
		var height = hasSettings ? config!.GetValue("video", "height", 0).AsInt32() : 0;

		return (mode, width, height);
	}

	public static void SaveVideoConfig(string mode, int width, int height)
	{
		var config = LoadOrCreate();
		config.SetValue("video", "mode", mode);
		config.SetValue("video", "width", width);
		config.SetValue("video", "height", height);
		config.Save(SettingsPath);
	}

	public static void ApplySavedVideoConfig()
	{
		var (mode, width, height) = ReadVideoConfig();
		ApplyVideoConfig(mode, NormalizeWindowedResolution(width, height));
	}

	public static void ApplyVideoConfig(string mode, Vector2I windowedSize)
	{
		var window = (Window)((SceneTree)Godot.Engine.GetMainLoop()).Root;

		if (mode == "windowed")
		{
			window.Mode = Window.ModeEnum.Windowed;
			window.ContentScaleSize = Vector2I.Zero;
			window.Size = windowedSize;
			window.MoveToCenter();
			return;
		}

		window.Mode = Window.ModeEnum.Fullscreen;
		window.ContentScaleSize = Vector2I.Zero;
	}

	public static Vector2I NormalizeWindowedResolution(int width, int height)
	{
		if (TryFindResolution(width, height, out var resolution))
			return resolution;

		return SupportedResolutions[0];
	}

	public static bool TryFindResolution(int width, int height, out Vector2I resolution)
	{
		foreach (var candidate in SupportedResolutions)
		{
			if (candidate.X == width && candidate.Y == height)
			{
				resolution = candidate;
				return true;
			}
		}

		resolution = default;
		return false;
	}

	public static bool TryFindResolutionIndex(int width, int height, out int index)
	{
		for (var i = 0; i < SupportedResolutions.Length; i++)
		{
			if (SupportedResolutions[i].X == width && SupportedResolutions[i].Y == height)
			{
				index = i;
				return true;
			}
		}

		index = 0;
		return false;
	}

	private static bool TryLoad(out ConfigFile? config)
	{
		config = new ConfigFile();
		return config.Load(SettingsPath) == Error.Ok;
	}

	private static ConfigFile LoadOrCreate()
	{
		var config = new ConfigFile();
		config.Load(SettingsPath);
		return config;
	}
}
