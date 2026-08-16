using Godot;
using GrimSpace.Core;

namespace GrimSpace.Presentation.Menu;

public partial class StartMenu : Control
{
	private const string SettingsPath = "user://settings.cfg";

	private static readonly Vector2I[] Resolutions =
	[
		new(2560, 1440),
		new(1920, 1080),
		new(1600, 900),
		new(1280, 720),
	];

	private Control _mainPanel = null!;
	private Control _settingsPanel = null!;
	private OptionButton _displayMode = null!;
	private OptionButton _resolution = null!;

	public override void _Ready()
	{
		_mainPanel = GetNode<Control>("%MainPanel");
		_settingsPanel = GetNode<Control>("%SettingsPanel");
		_displayMode = GetNode<OptionButton>("%DisplayMode");
		_resolution = GetNode<OptionButton>("%Resolution");

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
		ApplyVideoSettings(mode, new Vector2I(width, height));
	}

	private void LoadSettingsToUi()
	{
		var (mode, width, height) = ReadVideoConfig();
		_displayMode.Selected = mode == "windowed" ? 1 : 0;
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

		if (width <= 0 || height <= 0 || !TryFindResolution(width, height, out _))
		{
			width = Resolutions[0].X;
			height = Resolutions[0].Y;
		}

		return (mode, width, height);
	}

	private void SelectResolution(int width, int height)
	{
		if (TryFindResolution(width, height, out var index))
			_resolution.Selected = index;
		else
			_resolution.Selected = 0;
	}

	private static bool TryFindResolution(int width, int height, out int index)
	{
		for (var i = 0; i < Resolutions.Length; i++)
		{
			if (Resolutions[i].X == width && Resolutions[i].Y == height)
			{
				index = i;
				return true;
			}
		}

		index = 0;
		return false;
	}

	private Vector2I SelectedResolution() => Resolutions[_resolution.Selected];

	private void ApplyVideoSettings()
	{
		var windowed = _displayMode.Selected == 1;
		ApplyVideoSettings(windowed ? "windowed" : "fullscreen", SelectedResolution());
	}

	private void ApplyVideoSettings(string mode, Vector2I size)
	{
		var window = GetTree().Root;

		if (mode != "windowed")
		{
			window.Mode = Window.ModeEnum.Fullscreen;
			return;
		}

		window.Mode = Window.ModeEnum.Windowed;
		window.ContentScaleSize = size;
		window.Size = size;
		window.MoveToCenter();
	}

	private void SaveSettings()
	{
		var config = new ConfigFile();
		var windowed = _displayMode.Selected == 1;
		var size = windowed ? SelectedResolution() : Resolutions[0];

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
}
