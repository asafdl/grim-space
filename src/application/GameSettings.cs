using Godot;

namespace GrimSpace.Application;

public static class GameSettings
{
	private const string SettingsPath = "user://settings.cfg";
	private const string BorderlessFullscreenMode = "borderless_fullscreen";
	private const string WindowedMode = "windowed";

	public static readonly Vector2I DesignCanvasSize = new(1920, 1080);

	public static readonly Vector2I[] SupportedResolutions =
	[
		new(3840, 2160),
		new(2560, 1440),
		new(1920, 1080),
		new(1600, 900),
		new(1280, 720),
	];

	public enum DisplayMode
	{
		BorderlessFullscreen,
		Windowed,
	}

	public readonly record struct VideoConfig(
		DisplayMode Mode,
		Vector2I Resolution);

	public static VideoConfig ReadVideoConfig()
	{
		if (!TryLoad(out var config))
			return new VideoConfig(DisplayMode.BorderlessFullscreen, DefaultResolution());

		var mode = config!.GetValue("video", "mode", BorderlessFullscreenMode).AsString();
		var width = config.GetValue("video", "width", 0).AsInt32();
		var height = config.GetValue("video", "height", 0).AsInt32();
		var resolution = TryFindResolution(width, height, out var saved)
			? saved
			: DefaultResolution();

		return new VideoConfig(
			mode == WindowedMode ? DisplayMode.Windowed : DisplayMode.BorderlessFullscreen,
			resolution);
	}

	public static void SaveVideoConfig(VideoConfig video)
	{
		var config = LoadOrCreate();
		WriteVideoConfig(config, video);
		config.Save(SettingsPath);
	}

	internal static void WriteVideoConfig(ConfigFile config, VideoConfig video)
	{
		ArgumentNullException.ThrowIfNull(config);
		config.SetValue(
			"video",
			"mode",
			video.Mode == DisplayMode.Windowed ? WindowedMode : BorderlessFullscreenMode);
		config.SetValue("video", "width", video.Resolution.X);
		config.SetValue("video", "height", video.Resolution.Y);
		if (config.HasSectionKey("video", "render_scale"))
			config.EraseSectionKey("video", "render_scale");
	}

	public static void ApplySavedVideoConfig()
	{
		ApplyVideoConfig(ReadVideoConfig());
	}

	public static float ReadMasterVolume()
	{
		if (!TryLoad(out var config))
			return 1f;

		return Mathf.Clamp(config!.GetValue("audio", "master_volume", 1f).AsSingle(), 0f, 1f);
	}

	public static void SaveMasterVolume(float linear)
	{
		var config = LoadOrCreate();
		config.SetValue("audio", "master_volume", Mathf.Clamp(linear, 0f, 1f));
		config.Save(SettingsPath);
	}

	public static void ApplySavedAudioConfig() =>
		ApplyMasterVolume(ReadMasterVolume());

	public static bool ReadShowTutorials()
	{
		if (!TryLoad(out var config))
			return true;

		return config!.GetValue("gameplay", "show_tutorials", true).AsBool();
	}

	public static void SaveShowTutorials(bool enabled)
	{
		var config = LoadOrCreate();
		config.SetValue("gameplay", "show_tutorials", enabled);
		config.Save(SettingsPath);
	}

	public static void ApplyMasterVolume(float linear)
	{
		var bus = AudioServer.GetBusIndex("Master");
		AudioServer.SetBusVolumeDb(bus, linear <= 0f ? -80f : Mathf.LinearToDb(linear));
	}

	public static void ApplyVideoConfig(VideoConfig video)
	{
		var window = (Window)((SceneTree)Godot.Engine.GetMainLoop()).Root;
		var resolution = NormalizeResolution(video.Resolution.X, video.Resolution.Y);
		window.ContentScaleMode = Window.ContentScaleModeEnum.Viewport;
		window.ContentScaleAspect = Window.ContentScaleAspectEnum.Expand;
		window.ContentScaleSize = resolution;
		window.ContentScaleFactor = 1f;
		window.Scaling3DScale = 1f;

		if (video.Mode == DisplayMode.Windowed)
		{
			window.Mode = Window.ModeEnum.Windowed;
			window.Size = resolution;
			window.MoveToCenter();
			return;
		}

		window.Mode = Window.ModeEnum.Fullscreen;
	}

	public static Vector2I NormalizeResolution(int width, int height)
	{
		if (TryFindResolution(width, height, out var resolution))
			return resolution;

		return DesignCanvasSize;
	}

	public static Vector2I FitResolutionToScreen(Vector2I screenSize, float screenScale)
	{
		var scale = screenScale > 0f ? screenScale : 1f;
		var effectiveSize = new Vector2I(
			Mathf.RoundToInt(screenSize.X / scale),
			Mathf.RoundToInt(screenSize.Y / scale));

		foreach (var resolution in SupportedResolutions)
		{
			if (resolution.X <= effectiveSize.X && resolution.Y <= effectiveSize.Y)
				return resolution;
		}

		return SupportedResolutions[^1];
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

	private static Vector2I DefaultResolution() =>
		FitResolutionToScreen(DisplayServer.ScreenGetSize(), DisplayServer.ScreenGetScale());

	private static bool TryFindResolution(int width, int height, out Vector2I resolution)
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
