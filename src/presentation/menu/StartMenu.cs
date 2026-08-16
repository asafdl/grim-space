using Godot;
using GrimSpace.Core;

namespace GrimSpace.Presentation.Menu;

public partial class StartMenu : Control
{
	private const string SettingsPath = "user://settings.cfg";

	private static readonly Vector2I[] StandardResolutions =
	[
		new(1920, 1080),
		new(1600, 900),
		new(1280, 720),
	];

	private Control _mainPanel = null!;
	private Control _settingsPanel = null!;
	private OptionButton _displayMode = null!;
	private OptionButton _resolution = null!;
	private readonly List<Vector2I> _resolutionValues = [];

	public override void _Ready()
	{
		_mainPanel = GetNode<Control>("%MainPanel");
		_settingsPanel = GetNode<Control>("%SettingsPanel");
		_displayMode = GetNode<OptionButton>("%DisplayMode");
		_resolution = GetNode<OptionButton>("%Resolution");

		_displayMode.AddItem("Fullscreen");
		_displayMode.AddItem("Windowed");
		_displayMode.ItemSelected += _ => UpdateResolutionEnabled();

		GetNode<Button>("%StartBattle").Pressed += OnStartBattle;
		GetNode<Button>("%Settings").Pressed += ShowSettingsPanel;
		GetNode<Button>("%Back").Pressed += ShowMainPanel;
		GetNode<Button>("%Apply").Pressed += OnApply;
		GetNode<Button>("%Quit").Pressed += () => GetTree().Quit();

		CallDeferred(MethodName.InitializeVideoSettings);
	}

	private void InitializeVideoSettings()
	{
		var (mode, width, height) = ReadVideoConfig();
		ApplyVideoSettings(mode, width, height);
	}

	private void PopulateResolutions()
	{
		while (_resolution.ItemCount > 0)
			_resolution.RemoveItem(0);
		_resolutionValues.Clear();

		var native = GetNativeResolution();
		_resolution.AddItem($"Native — {native.X}x{native.Y}");
		_resolutionValues.Add(native);

		foreach (var resolution in StandardResolutions)
		{
			if (resolution == native)
				continue;

			_resolution.AddItem($"{resolution.X}x{resolution.Y}");
			_resolutionValues.Add(resolution);
		}
	}

	private void LoadSettingsToUi()
	{
		var (mode, width, height) = ReadVideoConfig();
		_displayMode.Selected = mode == "windowed" ? 1 : 0;
		PopulateResolutions();
		SelectResolution(width, height);
		UpdateResolutionEnabled();
	}

	private (string Mode, int Width, int Height) ReadVideoConfig()
	{
		var config = new ConfigFile();
		var hasSettings = config.Load(SettingsPath) == Error.Ok;

		var mode = hasSettings
			? config.GetValue("video", "mode", "fullscreen").AsString()
			: "fullscreen";
		var width = hasSettings ? config.GetValue("video", "width", 0).AsInt32() : 0;
		var height = hasSettings ? config.GetValue("video", "height", 0).AsInt32() : 0;

		var native = GetNativeResolution();
		if (width <= 0 || height <= 0 || width > native.X || height > native.Y)
		{
			width = native.X;
			height = native.Y;
		}

		return (mode, width, height);
	}

	private void SelectResolution(int width, int height)
	{
		for (var i = 0; i < _resolutionValues.Count; i++)
		{
			if (_resolutionValues[i].X == width && _resolutionValues[i].Y == height)
			{
				_resolution.Selected = i;
				return;
			}
		}

		_resolution.Selected = 0;
	}

	private void ApplyVideoSettings()
	{
		var windowed = _displayMode.Selected == 1;
		var size = _resolutionValues[_resolution.Selected];
		ApplyVideoSettings(windowed ? "windowed" : "fullscreen", size.X, size.Y);
	}

	private void ApplyVideoSettings(string mode, int width, int height)
	{
		var window = GetWindow();
		var windowed = mode == "windowed";
		var native = GetNativeResolution();

		if (width <= 0 || height <= 0 || width > native.X || height > native.Y)
		{
			width = native.X;
			height = native.Y;
		}

		if (!windowed)
		{
			window.Mode = Window.ModeEnum.Fullscreen;
			return;
		}

		window.Mode = Window.ModeEnum.Windowed;
		window.Size = ToWindowSize(new Vector2I(width, height));
		window.MoveToCenter();
	}

	private void SaveSettings()
	{
		var config = new ConfigFile();
		var windowed = _displayMode.Selected == 1;
		var native = GetNativeResolution();
		var size = windowed ? _resolutionValues[_resolution.Selected] : native;

		config.SetValue("video", "mode", windowed ? "windowed" : "fullscreen");
		config.SetValue("video", "width", size.X);
		config.SetValue("video", "height", size.Y);
		config.Save(SettingsPath);
	}

	private void UpdateResolutionEnabled() =>
		_resolution.Disabled = _displayMode.Selected != 1;

	private void OnApply()
	{
		ApplyVideoSettings();
		SaveSettings();
	}

	private void ShowSettingsPanel()
	{
		_mainPanel.Visible = false;
		_settingsPanel.Visible = true;
		LoadSettingsToUi();
	}

	private void ShowMainPanel()
	{
		_settingsPanel.Visible = false;
		_mainPanel.Visible = true;
	}

	private void OnStartBattle()
	{
		RunSession.Instance.StartNewRun();
		GetTree().ChangeSceneToFile("res://scenes/battle.tscn");
	}

	private Vector2I GetNativeResolution()
	{
		var best = Vector2I.Zero;
		for (var screen = 0; screen < DisplayServer.GetScreenCount(); screen++)
		{
			var size = GetPhysicalScreenSize(screen);
			if (size.X > best.X || (size.X == best.X && size.Y > best.Y))
				best = size;
		}

		return best.X > 0 && best.Y > 0 ? best : new Vector2I(1920, 1080);
	}

	private static Vector2I GetPhysicalScreenSize(int screen)
	{
		var size = DisplayServer.ScreenGetSize(screen);
		var scale = DisplayServer.ScreenGetScale(screen);
		if (scale > 1.01f)
			size = new Vector2I(
				(int)System.Math.Round(size.X * scale),
				(int)System.Math.Round(size.Y * scale));

		return size;
	}

	private Vector2I ToWindowSize(Vector2I physicalSize)
	{
		var screen = GetWindow().CurrentScreen;
		if (screen < 0)
			screen = DisplayServer.GetPrimaryScreen();
		if (screen < 0)
			screen = 0;

		var scale = DisplayServer.ScreenGetScale(screen);
		if (scale <= 1.01f)
			return physicalSize;

		return new Vector2I(
			(int)System.Math.Round(physicalSize.X / scale),
			(int)System.Math.Round(physicalSize.Y / scale));
	}
}
