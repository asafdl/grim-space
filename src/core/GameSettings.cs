using Godot;

namespace GrimSpace.Core;

public static class GameSettings
{
	private const string SettingsPath = "user://settings.cfg";

	public static bool ShowIntro
	{
		get => GetValue("game", "show_intro", true).AsBool();
		set => SetValue("game", "show_intro", value);
	}

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

	private static Variant GetValue(string section, string key, Variant defaultValue)
	{
		if (!TryLoad(out var config))
			return defaultValue;

		return config!.GetValue(section, key, defaultValue);
	}

	private static void SetValue(string section, string key, Variant value)
	{
		var config = LoadOrCreate();
		config.SetValue(section, key, value);
		config.Save(SettingsPath);
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
