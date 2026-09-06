using Godot;
using GrimSpace.Core;

namespace GrimSpace.Presentation.Menu;

public partial class StartMenu : Control
{
	private Control _mainPanel = null!;
	private Control _settingsPanel = null!;
	private OptionButton _displayMode = null!;
	private OptionButton _resolution = null!;
	private CheckBox _showIntro = null!;

	public override void _Ready()
	{
		_mainPanel = GetNode<Control>("%MainPanel");
		_settingsPanel = GetNode<Control>("%SettingsPanel");
		_displayMode = GetNode<OptionButton>("%DisplayMode");
		_resolution = GetNode<OptionButton>("%Resolution");
		_showIntro = GetNode<CheckBox>("%ShowIntro");

		_displayMode.ItemSelected += _ => UpdateResolutionEnabled();
		_showIntro.Toggled += OnShowIntroToggled;

		GetNode<Button>("%StartBattle").Pressed += OnStartBattle;
		GetNode<Button>("%Settings").Pressed += ShowSettingsPanel;
		GetNode<Button>("%Back").Pressed += ShowMainPanel;
		GetNode<Button>("%Apply").Pressed += OnApply;
		GetNode<Button>("%Quit").Pressed += () => GetTree().Quit();

		_showIntro.ButtonPressed = GameSettings.ShowIntro;
	}

	private void LoadSettingsToUi()
	{
		var (mode, width, height) = GameSettings.ReadVideoConfig();
		_displayMode.Selected = mode == "windowed" ? 1 : 0;
		SelectResolution(width, height);
		UpdateResolutionEnabled();
	}

	private void SelectResolution(int width, int height)
	{
		if (GameSettings.TryFindResolutionIndex(width, height, out var index))
			_resolution.Selected = index;
		else
			_resolution.Selected = 0;
	}

	private Vector2I SelectedResolution() => GameSettings.SupportedResolutions[_resolution.Selected];

	private void ApplyVideoSettings()
	{
		var windowed = _displayMode.Selected == 1;
		GameSettings.ApplyVideoConfig(
			windowed ? "windowed" : "fullscreen",
			SelectedResolution());
	}

	private void SaveVideoSettings()
	{
		var windowed = _displayMode.Selected == 1;
		var size = windowed
			? SelectedResolution()
			: GameSettings.SupportedResolutions[0];

		GameSettings.SaveVideoConfig(
			windowed ? "windowed" : "fullscreen",
			size.X,
			size.Y);
	}

	private void UpdateResolutionEnabled() =>
		_resolution.Disabled = _displayMode.Selected != 1;

	private void OnApply()
	{
		ApplyVideoSettings();
		SaveVideoSettings();
	}

	private void OnShowIntroToggled(bool enabled) =>
		GameSettings.ShowIntro = enabled;

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
