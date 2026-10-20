using Godot;
using GrimSpace.Application;

namespace GrimSpace.Presentation.Menu;

public partial class GameSettingsMenu : Control
{
	public event Action? CloseRequested;

	public GameSettingsApplicator.Host Host { get; set; } = GameSettingsApplicator.Host.InRun;

	private OptionButton _displayMode = null!;
	private OptionButton _resolution = null!;
	private OptionButton _uiScale = null!;
	private HSlider _masterVolume = null!;
	private HSlider _musicVolume = null!;
	private HSlider _sfxVolume = null!;
	private CheckBox _showTutorials = null!;
	private CheckBox _mouseCameraPan = null!;
	private CheckBox _showActionLog = null!;
	private KeyBindingsTab _keyBindingsTab = null!;
	private TabContainer _settingsTabs = null!;
	private PanelContainer _settingsPanelFrame = null!;

	private enum SettingsTab
	{
		Video = 0,
		Audio = 1,
		Gameplay = 2,
		KeyBindings = 3,
	}

	public override void _Ready()
	{
		MenuUi.ApplyTheme(this);
		_displayMode = GetNode<OptionButton>("%DisplayMode");
		_resolution = GetNode<OptionButton>("%Resolution");
		_uiScale = GetNode<OptionButton>("%UiScale");
		_masterVolume = GetNode<HSlider>("%MasterVolume");
		_musicVolume = GetNode<HSlider>("%MusicVolume");
		_sfxVolume = GetNode<HSlider>("%SfxVolume");
		_showTutorials = GetNode<CheckBox>("%ShowTutorials");
		_mouseCameraPan = GetNode<CheckBox>("%MouseCameraPan");
		_showActionLog = GetNode<CheckBox>("%ShowActionLog");
		_keyBindingsTab = GetNode<KeyBindingsTab>("%Keys");
		_settingsTabs = GetNode<TabContainer>("%SettingsTabs");
		_settingsPanelFrame = GetNode<PanelContainer>("%Panel");
		_settingsTabs.SetTabTitle((int)SettingsTab.KeyBindings, "Key Bindings");

		PopulateResolutions();
		PopulateUiScales();
		GetViewport().SizeChanged += OnViewportSizeChanged;

		GetNode<Button>("%Apply").Pressed += OnApply;
		GetNode<Button>("%Back").Pressed += () => CloseRequested?.Invoke();
		GetNode<Button>("%Reset").Pressed += OnResetActiveTab;
		_displayMode.ItemSelected += _ => UpdateResolutionAvailability();

		CallDeferred(MethodName.FitSettingsPanelToViewport);
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (!Visible)
			return;

		if (@event is not InputEventKey { Pressed: true, Echo: false, Keycode: Key.Escape })
			return;

		CloseRequested?.Invoke();
		GetViewport().SetInputAsHandled();
	}

	public void Open()
	{
		FitSettingsPanelToViewport();
		LoadSettingsToUi();
		_keyBindingsTab.LoadDraft(GameSettings.ReadKeyBindings());
		Show();
	}

	private void OnApply()
	{
		var snapshot = CaptureUiSnapshot();
		if (!GameSettingsApplicator.TryCommit(snapshot, Host, out var error))
		{
			_keyBindingsTab.ShowStatus(error ?? "Could not apply settings.");
			return;
		}

		_keyBindingsTab.ShowStatus(string.Empty);
		CallDeferred(MethodName.FitSettingsPanelToViewport);
	}

	private void LoadSettingsToUi()
	{
		var video = GameSettings.ReadVideoConfig();
		_displayMode.Selected = video.Mode == GameSettings.DisplayMode.Windowed ? 1 : 0;
		SelectResolution(video.Resolution);
		SelectUiScale(video.UiScale);
		UpdateResolutionAvailability();
		var audio = GameSettings.ReadAudioConfig();
		_masterVolume.Value = audio.MasterVolume * 100f;
		_musicVolume.Value = audio.MusicVolume * 100f;
		_sfxVolume.Value = audio.SfxVolume * 100f;
		_showTutorials.ButtonPressed = GameSettings.ReadShowTutorials();
		_mouseCameraPan.ButtonPressed = GameSettings.ReadMouseCameraPan();
		_showActionLog.ButtonPressed = GameSettings.ReadShowActionLog();
	}

	private GameSettingsSessionSnapshot CaptureUiSnapshot() =>
		new(
			SelectedVideoConfig(),
			SelectedAudioConfig(),
			_showTutorials.ButtonPressed,
			_mouseCameraPan.ButtonPressed,
			_showActionLog.ButtonPressed,
			_keyBindingsTab.CurrentDraft);

	private void PopulateResolutions()
	{
		_resolution.Clear();
		foreach (var resolution in GameSettings.SupportedResolutions)
			_resolution.AddItem($"{resolution.X}x{resolution.Y}");
	}

	private void PopulateUiScales()
	{
		_uiScale.Clear();
		foreach (var scale in GameSettings.SupportedUiScales)
			_uiScale.AddItem($"{scale * 100f:0}%");
	}

	private void SelectResolution(Vector2I resolution)
	{
		_resolution.Selected = GameSettings.TryFindResolutionIndex(
			resolution.X,
			resolution.Y,
			out var index)
			? index
			: 0;
	}

	private void SelectUiScale(float scale)
	{
		var normalized = GameSettings.NormalizeUiScale(scale);
		for (var i = 0; i < GameSettings.SupportedUiScales.Length; i++)
		{
			if (Mathf.IsEqualApprox(GameSettings.SupportedUiScales[i], normalized))
			{
				_uiScale.Selected = i;
				return;
			}
		}

		_uiScale.Selected = 0;
	}

	private GameSettings.VideoConfig SelectedVideoConfig() =>
		new(SelectedDisplayMode(), SelectedResolution(), SelectedUiScale());

	private GameSettings.DisplayMode SelectedDisplayMode() =>
		_displayMode.Selected == 1
			? GameSettings.DisplayMode.Windowed
			: GameSettings.DisplayMode.BorderlessFullscreen;

	private Vector2I SelectedResolution() =>
		GameSettings.SupportedResolutions[_resolution.Selected];

	private float SelectedUiScale() =>
		GameSettings.SupportedUiScales[_uiScale.Selected];

	private GameSettings.AudioConfig SelectedAudioConfig() =>
		new(
			(float)(_masterVolume.Value / 100.0),
			(float)(_musicVolume.Value / 100.0),
			(float)(_sfxVolume.Value / 100.0));

	private void OnResetActiveTab()
	{
		switch ((SettingsTab)_settingsTabs.CurrentTab)
		{
			case SettingsTab.Video:
				ResetVideoUiToDefaults();
				break;
			case SettingsTab.Audio:
				var audioDefaults = GameSettings.DefaultAudioConfig;
				_masterVolume.Value = audioDefaults.MasterVolume * 100f;
				_musicVolume.Value = audioDefaults.MusicVolume * 100f;
				_sfxVolume.Value = audioDefaults.SfxVolume * 100f;
				break;
			case SettingsTab.Gameplay:
				_showTutorials.ButtonPressed = true;
				_mouseCameraPan.ButtonPressed = true;
				_showActionLog.ButtonPressed = true;
				break;
			case SettingsTab.KeyBindings:
				_keyBindingsTab.ResetToDefaults();
				break;
		}
	}

	private void ResetVideoUiToDefaults()
	{
		_displayMode.Selected = 0;
		SelectResolution(GameSettings.FitResolutionToScreen(
			DisplayServer.ScreenGetSize(),
			DisplayServer.ScreenGetScale()));
		SelectUiScale(1f);
		UpdateResolutionAvailability();
	}

	private void UpdateResolutionAvailability() =>
		_resolution.Disabled = SelectedDisplayMode() == GameSettings.DisplayMode.BorderlessFullscreen;

	private void FitSettingsPanelToViewport()
	{
		const float maxWidth = 880f;
		const float maxHeight = 600f;
		const float margin = 40f;
		var viewport = GetViewport().GetVisibleRect().Size;
		_settingsPanelFrame.CustomMinimumSize = new Vector2I(
			(int)Mathf.Min(maxWidth, viewport.X - margin),
			(int)Mathf.Min(maxHeight, viewport.Y - margin));
	}

	private void OnViewportSizeChanged() => FitSettingsPanelToViewport();
}
