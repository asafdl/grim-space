using Godot;
using GrimSpace.Application;

namespace GrimSpace.Presentation.Menu;

public partial class StartMenu : Control
{
	private const string IntroScenePath = "res://scenes/intro.tscn";
	private const string DiscordInviteUrl = "https://discord.gg/dMPX9SH3Dd";
	private static readonly string[] LoadingMessages =
	[
		"Lighting galaxy on fire...",
		"Scrapping all rust boxes...",
		"Cooking neuro-drugs...",
		"Counting suspicious oxygen tanks...",
		"Polishing the escape pods...",
		"Hunting down any remaining humans...",
		"Grooming Syndi's beard..."
	];

	[Export(PropertyHint.Range, "0.005,0.05,0.005")]
	private float _dustBandEndHalfThicknessRatio = 0.015f;

	private Control _menuColumn = null!;
	private Control _settingsOverlay = null!;
	private Control _loadingOverlay = null!;
	private Control _loadingGear = null!;
	private Label _loadingLabel = null!;
	private int _loadingMessageIndex = -1;
	private double _loadingMessageNextChangeAt;
	private Control _art = null!;
	private Control _dustBandStart = null!;
	private Control _dustBandEnd = null!;
	private Control _dustBandWide = null!;
	private GpuParticles2D _dustParticles = null!;
	private ParticleProcessMaterial _dustMaterial = null!;
	private OptionButton _displayMode = null!;
	private OptionButton _resolution = null!;
	private OptionButton _uiScale = null!;
	private HSlider _masterVolume = null!;
	private HSlider _musicVolume = null!;
	private HSlider _sfxVolume = null!;
	private CheckBox _showTutorials = null!;
	private CheckBox _mouseCameraPan = null!;
	private KeyBindingsTab _keyBindingsTab = null!;
	private TabContainer _settingsTabs = null!;
	private PanelContainer _settingsPanelFrame = null!;
	private Button _resetSettings = null!;
	private Button _newGameButton = null!;
	private Button _continueButton = null!;

	private enum SettingsTab
	{
		Video = 0,
		Audio = 1,
		Gameplay = 2,
		KeyBindings = 3,
	}

	public override void _Ready()
	{
		GetNode<Label>("%VersionLabel").Text = $"v{GameVersion.Display}";

		_menuColumn = GetNode<Control>("%MenuColumn");
		_settingsOverlay = GetNode<Control>("%SettingsOverlay");
		_loadingOverlay = GetNode<Control>("%LoadingOverlay");
		_loadingGear = GetNode<Control>("%LoadingGear");
		_loadingLabel = GetNode<Label>("%LoadingLabel");
		GetViewport().SizeChanged += OnViewportSizeChanged;
		_art = GetNode<Control>("ArtFrame/Art");
		_dustBandStart = GetNode<Control>("%DustBandStart");
		_dustBandEnd = GetNode<Control>("%DustBandEnd");
		_dustBandWide = GetNode<Control>("%DustBandWide");
		_dustParticles = GetNode<GpuParticles2D>("%DustParticles");
		_dustMaterial = (ParticleProcessMaterial)_dustParticles.ProcessMaterial.Duplicate();
		_dustParticles.ProcessMaterial = _dustMaterial;
		_art.Resized += OnDustLayoutChanged;
		CallDeferred(MethodName.UpdateDustLayout);

		_displayMode = GetNode<OptionButton>("%DisplayMode");
		_resolution = GetNode<OptionButton>("%Resolution");
		_uiScale = GetNode<OptionButton>("%UiScale");
		_masterVolume = GetNode<HSlider>("%MasterVolume");
		_musicVolume = GetNode<HSlider>("%MusicVolume");
		_sfxVolume = GetNode<HSlider>("%SfxVolume");
		_showTutorials = GetNode<CheckBox>("%ShowTutorials");
		_mouseCameraPan = GetNode<CheckBox>("%MouseCameraPan");
		_keyBindingsTab = GetNode<KeyBindingsTab>("%Keys");
		_settingsTabs = GetNode<TabContainer>("%SettingsTabs");
		_settingsPanelFrame = GetNode<PanelContainer>("%Panel");
		_settingsTabs.SetTabTitle((int)SettingsTab.KeyBindings, "Key Bindings");
		_resetSettings = GetNode<Button>("%Reset");
		_resetSettings.Pressed += OnResetActiveTab;
		_displayMode.ItemSelected += OnDisplayModeSelected;

		PopulateResolutions();
		PopulateUiScales();
		CallDeferred(MethodName.FitSettingsPanelToViewport);

		_newGameButton = GetNode<Button>("%NewGame");
		_continueButton = GetNode<Button>("%Continue");
		_continueButton.Disabled = !Session.Instance.HasSaveGame;
		GetNode<Button>("%PlayIntro").Pressed += OnPlayIntro;
		_newGameButton.Pressed += OnNewGame;
		_continueButton.Pressed += OnContinue;
		GetNode<Button>("%Settings").Pressed += ShowSettingsPanel;
		GetNode<Button>("%Back").Pressed += ShowMainPanel;
		GetNode<Button>("%Apply").Pressed += OnApply;
		GetNode<Button>("%Quit").Pressed += () => GetTree().Quit();
		var discord = GetNode<TextureButton>("%Discord");
		discord.MouseDefaultCursorShape = CursorShape.PointingHand;
		discord.Pressed += OnDiscordPressed;

		CallDeferred(MethodName.PrepareFirstScene);
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (@event is not InputEventKey { Pressed: true, Echo: false, Keycode: Key.Escape }
			|| !_settingsOverlay.Visible)
			return;

		ShowMainPanel();
		GetViewport().SetInputAsHandled();
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
	}

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

	private void OnApply()
	{
		if (!_keyBindingsTab.TryGetCommittedBindings(out var bindings, out var bindingError))
		{
			_keyBindingsTab.ShowStatus(bindingError ?? "Invalid key bindings.");
			return;
		}

		var video = SelectedVideoConfig();
		var audio = SelectedAudioConfig();
		var showTutorials = _showTutorials.ButtonPressed;
		var mouseCameraPan = _mouseCameraPan.ButtonPressed;
		var tutorialsSettingChanged = showTutorials != GameSettings.ReadShowTutorials();
		var saveError = GameSettings.SaveAll(
			video,
			audio,
			showTutorials,
			mouseCameraPan,
			bindings);
		if (saveError != Error.Ok)
		{
			_keyBindingsTab.ShowStatus("Could not save settings to disk.");
			return;
		}

		GameSettings.ApplyVideoConfig(video);
		GameSettings.ApplyAudioConfig(audio);
		GameSettings.ApplyMouseCameraPan(mouseCameraPan);
		GameInputBindings.Apply(bindings);
		CallDeferred(MethodName.FitSettingsPanelToViewport);
		if (tutorialsSettingChanged)
		{
			Session.Instance.DiscardPreparedRun();
			Session.Instance.PrepareFirstScene();
		}

		_keyBindingsTab.ShowStatus(string.Empty);
	}

	private void ShowSettingsPanel()
	{
		FitSettingsPanelToViewport();
		LoadSettingsToUi();
		_keyBindingsTab.LoadDraft(GameSettings.ReadKeyBindings());
		_menuColumn.Hide();
		_settingsOverlay.Show();
	}

	private void OnResetActiveTab()
	{
		switch ((SettingsTab)_settingsTabs.CurrentTab)
		{
			case SettingsTab.Video:
				ResetVideoUiToDefaults();
				break;
			case SettingsTab.Audio:
				_masterVolume.Value = 100f;
				_musicVolume.Value = 100f;
				_sfxVolume.Value = 100f;
				break;
			case SettingsTab.Gameplay:
				_showTutorials.ButtonPressed = true;
				_mouseCameraPan.ButtonPressed = true;
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

	private void OnDisplayModeSelected(long _index) => UpdateResolutionAvailability();

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

	private void ShowMainPanel()
	{
		_settingsOverlay.Hide();
		_menuColumn.Show();
	}

	private void OnPlayIntro() =>
		GetTree().ChangeSceneToFile(IntroScenePath);

	private static void OnDiscordPressed() =>
		OS.ShellOpen(DiscordInviteUrl);

	private void PrepareFirstScene() =>
		Session.Instance.PrepareFirstScene();

	private async void OnNewGame()
	{
		_newGameButton.Disabled = true;
		_menuColumn.Hide();
		_loadingOverlay.Show();
		SetLoadingMessage();
		SetProcess(true);
		try
		{
			await Session.Instance.BeginMapFromMenuAsync();
		}
		catch (Exception ex)
		{
			GD.PrintErr($"Failed to start map: {ex}");
			_newGameButton.Disabled = false;
			_loadingOverlay.Hide();
			_menuColumn.Show();
		}
	}

	private void OnContinue()
	{
		_continueButton.Disabled = true;
		_newGameButton.Disabled = true;
		_menuColumn.Hide();
		_loadingOverlay.Show();
		_loadingLabel.Text = "Restoring run...";

		var result = Session.Instance.TryLoadGame();
		if (result == GrimSpace.Run.Persistence.LoadResult.Success)
			return;

		_newGameButton.Disabled = false;
		_continueButton.Disabled = !Session.Instance.HasSaveGame;
		_loadingOverlay.Hide();
		_menuColumn.Show();
		GD.PrintErr($"Failed to load save game: {result}");
	}

	public override void _Process(double _delta)
	{
		if (!_loadingOverlay.Visible)
			return;

		_loadingGear.Rotation += Mathf.DegToRad(90f) * (float)_delta;
		if (Time.GetTicksMsec() / 1000.0 >= _loadingMessageNextChangeAt)
			SetLoadingMessage();
	}

	private void SetLoadingMessage()
	{
		var nextIndex = Random.Shared.Next(LoadingMessages.Length);
		while (LoadingMessages.Length > 1 && nextIndex == _loadingMessageIndex)
			nextIndex = Random.Shared.Next(LoadingMessages.Length);

		_loadingMessageIndex = nextIndex;
		_loadingLabel.Text = LoadingMessages[nextIndex];
		_loadingMessageNextChangeAt = Time.GetTicksMsec() / 1000.0 + 2.5;
	}

	private void OnDustLayoutChanged() => UpdateDustLayout();

	private void UpdateDustLayout()
	{
		var artSize = _art.Size;
		if (artSize.X < 1f || artSize.Y < 1f)
			return;

		var start = _dustBandStart.Position;
		var end = _dustBandEnd.Position;
		var wide = _dustBandWide.Position;
		var delta = end - start;
		var length = delta.Length();
		if (length < 1f)
			return;

		var tangent = delta / length;
		var normal = new Vector2(-tangent.Y, tangent.X);
		var startHalf = Mathf.Abs((wide - start).Dot(normal));
		var endHalf = artSize.Y * _dustBandEndHalfThicknessRatio;
		var bandHalfThickness = (startHalf + endHalf) * 0.5f;

		var center = (start + end) * 0.5f;
		var visibilityPad = length * 0.5f + bandHalfThickness + 64f;

		_dustParticles.Position = center;
		_dustParticles.Rotation = delta.Angle();
		_dustMaterial.EmissionBoxExtents = new Vector3(length * 0.5f, bandHalfThickness, 1f);
		_dustParticles.VisibilityRect = new Rect2(
			-visibilityPad,
			-visibilityPad,
			visibilityPad * 2f,
			visibilityPad * 2f);
	}
}
