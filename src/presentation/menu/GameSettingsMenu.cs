using Godot;
using GrimSpace.Application;
using GrimSpace.Components;
using GrimSpace.Ui.Components;

namespace GrimSpace.Presentation.Menu;

public partial class GameSettingsMenu : Control
{
	public event Action? CloseRequested;

	public GameSettingsApplicator.Host Host { get; set; } = GameSettingsApplicator.Host.InRun;

	private UiSelect _displayMode = null!;
	private UiSelect _resolution = null!;
	private UiSelect _uiScale = null!;
	private UiSlider _masterVolume = null!;
	private UiSlider _musicVolume = null!;
	private UiSlider _sfxVolume = null!;
	private UiToggle _showTutorials = null!;
	private UiToggle _mouseCameraPan = null!;
	private UiToggle _showActionLog = null!;
	private KeyBindingsTab _keyBindingsTab = null!;
	private ModalShell _shell = null!;
	private Control _unsavedChangesPopup = null!;
	private Label _unsavedChangesStatus = null!;
	private TabContainer _settingsTabs = null!;
	private GameSettingsSessionSnapshot _baselineSnapshot;
	private bool _hasBaselineSnapshot;

	private enum SettingsTab
	{
		Video = 0,
		Audio = 1,
		Gameplay = 2,
		KeyBindings = 3,
	}

	public override void _Ready()
	{
		HudThemes.Apply(this, HudThemeFamily.Tutorial);
		_shell = new ModalShell(
			UiContainerVisual.Modal,
			HudThemeFamily.Tutorial,
			HudPalettes.PurpleRed);
		_shell.SetHeader(HudHeaderMode.TitleOnly);
		_shell.SetDismissVisible(true);
		_shell.SetFooter(
		[
			new HudAction("Reset", HudActionKind.Secondary, OnResetActiveTab),
			new HudAction("Apply", HudActionKind.Primary, OnApply),
		]);
		AddChild(_shell);

		BuildSettingsContent();
		BuildUnsavedChangesDialog();
		PopulateResolutions();
		PopulateUiScales();
		GetViewport().SizeChanged += OnViewportSizeChanged;
		CallDeferred(MethodName.FitSettingsPanelToViewport);
	}

	public void Open()
	{
		FitSettingsPanelToViewport();
		LoadSettingsToUi();
		_keyBindingsTab.LoadDraft(GameSettings.ReadKeyBindings());
		_unsavedChangesPopup.Hide();
		_baselineSnapshot = CaptureUiSnapshot();
		_hasBaselineSnapshot = true;
		_shell.SetEscapeHandlingEnabled(true);
		_shell.SetBackHandler(OnEscapeRequested);
		_shell.SetCloseRequestHandler(OnCloseRequested);
		_shell.SetDismissOnBackdropClick(false);
		_shell.Open("SETTINGS");
		Show();
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (TryHandleEscape(@event))
			GetViewport().SetInputAsHandled();
	}

	public bool TryHandleEscape(InputEvent @event)
	{
		if (!Visible
			|| @event is not InputEventKey { Pressed: true, Echo: false, Keycode: Key.Escape })
			return false;

		if (_unsavedChangesPopup.Visible)
		{
			_unsavedChangesPopup.Hide();
			_shell.SetEscapeHandlingEnabled(true);
			return true;
		}

		return _shell.TryHandleInput(@event);
	}

	private void BuildUnsavedChangesDialog()
	{
		_unsavedChangesPopup = new Control
		{
			Name = "UnsavedChangesPopup",
			AnchorsPreset = (int)LayoutPreset.FullRect,
			AnchorRight = 1f,
			AnchorBottom = 1f,
			GrowHorizontal = GrowDirection.Both,
			GrowVertical = GrowDirection.Both,
			MouseFilter = MouseFilterEnum.Stop,
			Visible = false,
		};
		var center = new CenterContainer
		{
			AnchorsPreset = (int)LayoutPreset.FullRect,
			AnchorRight = 1f,
			AnchorBottom = 1f,
			GrowHorizontal = GrowDirection.Both,
			GrowVertical = GrowDirection.Both,
			MouseFilter = MouseFilterEnum.Ignore,
		};
		_unsavedChangesPopup.AddChild(center);

		var frame = new UiContainer(HudPalettes.PurpleRed)
		{
			SizeOverride = new UiSizeOverride(600f, 300f),
		};
		frame.SetVisual(UiContainerVisual.Technical);
		center.AddChild(frame);

		var margin = new MarginContainer
		{
			AnchorsPreset = (int)LayoutPreset.FullRect,
			AnchorRight = 1f,
			AnchorBottom = 1f,
		};
		margin.AddThemeConstantOverride("margin_left", frame.ContentMarginLeft);
		margin.AddThemeConstantOverride("margin_top", frame.ContentMarginTop + 24);
		margin.AddThemeConstantOverride("margin_right", frame.ContentMarginRight);
		margin.AddThemeConstantOverride("margin_bottom", frame.ContentMarginBottom);
		frame.AddChild(margin);

		var content = new VBoxContainer
		{
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
			SizeFlagsVertical = SizeFlags.ExpandFill,
		};
		content.AddThemeConstantOverride("separation", 24);
		margin.AddChild(content);

		_unsavedChangesStatus = new Label
		{
			Text = "Apply your settings changes before exiting?",
			AutowrapMode = TextServer.AutowrapMode.WordSmart,
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Center,
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
		};
		content.AddChild(_unsavedChangesStatus);
		content.AddChild(new Control { SizeFlagsVertical = SizeFlags.ExpandFill });

		var actions = new HBoxContainer
		{
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
		};
		actions.AddThemeConstantOverride("separation", 12);
		var cancel = new UiButton("Cancel", HudActionKind.Secondary, HudPalettes.PurpleRed)
		{
			SizeFlagsHorizontal = SizeFlags.ShrinkCenter,
		};
		cancel.SetVisual(UiButtonVisual.WideBeveled);
		cancel.Pressed += DiscardAndExit;
		var apply = new UiButton("Apply", HudActionKind.Primary, HudPalettes.PurpleRed)
		{
			SizeFlagsHorizontal = SizeFlags.ShrinkCenter,
		};
		apply.SetVisual(UiButtonVisual.WideBeveled);
		apply.Pressed += ApplyAndExit;
		actions.AddChild(cancel);
		actions.AddChild(new Control { SizeFlagsHorizontal = SizeFlags.ExpandFill });
		actions.AddChild(apply);
		content.AddChild(actions);
		AddChild(_unsavedChangesPopup);
	}

	private void BuildSettingsContent()
	{
		var content = new VBoxContainer();
		content.AddThemeConstantOverride("separation", 16);

		var tabBar = new HBoxContainer();
		tabBar.AddThemeConstantOverride("separation", 12);
		var group = new ButtonGroup();
		foreach (var (title, index) in new[]
		{
			("Video", (int)SettingsTab.Video),
			("Audio", (int)SettingsTab.Audio),
			("Gameplay", (int)SettingsTab.Gameplay),
			("Key Bindings", (int)SettingsTab.KeyBindings),
		})
		{
			var tab = new UiTabButton(title, HudPalettes.PurpleRed) { ButtonGroup = group };
			tab.SetSelected(index == (int)SettingsTab.Video);
			tab.Pressed += () => _settingsTabs.CurrentTab = index;
			tabBar.AddChild(tab);
		}
		content.AddChild(tabBar);

		_settingsTabs = new TabContainer
		{
			TabsVisible = false,
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
			SizeFlagsVertical = SizeFlags.ExpandFill,
			CustomMinimumSize = new Vector2(0f, 330f),
		};
		_settingsTabs.TabChanged += OnSettingsTabChanged;
		_settingsTabs.AddChild(BuildVideoTab());
		_settingsTabs.AddChild(BuildAudioTab());
		_settingsTabs.AddChild(BuildGameplayTab());
		_keyBindingsTab = new KeyBindingsTab
		{
			Name = "KeyBindings",
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
			SizeFlagsVertical = SizeFlags.ExpandFill,
		};
		_settingsTabs.AddChild(_keyBindingsTab);
		_shell.SetBodyScrollingEnabled(false);
		content.AddChild(_settingsTabs);
		_shell.SetBody(content);
	}

	private Control BuildVideoTab()
	{
		var page = CreateSettingsPage();
		_displayMode = CreateSelect();
		_displayMode.AddItem("Borderless Fullscreen");
		_displayMode.AddItem("Windowed");
		_displayMode.ItemSelected += _ => UpdateResolutionAvailability();
		page.AddChild(CreateSettingRow("Display mode", _displayMode));

		_resolution = CreateSelect();
		page.AddChild(CreateSettingRow("Resolution", _resolution));

		_uiScale = CreateSelect();
		page.AddChild(CreateSettingRow("UI scale", _uiScale));
		return page;
	}

	private Control BuildAudioTab()
	{
		var page = CreateSettingsPage();
		_masterVolume = CreateVolumeSlider();
		page.AddChild(CreateSettingRow("Master volume", _masterVolume));
		_musicVolume = CreateVolumeSlider();
		page.AddChild(CreateSettingRow("Music volume", _musicVolume));
		_sfxVolume = CreateVolumeSlider();
		page.AddChild(CreateSettingRow("SFX volume", _sfxVolume));
		return page;
	}

	private Control BuildGameplayTab()
	{
		var page = CreateSettingsPage();
		_showTutorials = new UiToggle(palette: HudPalettes.PurpleRed);
		page.AddChild(CreateSettingRow("Show tutorials", _showTutorials));
		_mouseCameraPan = new UiToggle(palette: HudPalettes.PurpleRed);
		page.AddChild(CreateSettingRow("Pan camera with mouse", _mouseCameraPan));
		_showActionLog = new UiToggle(palette: HudPalettes.PurpleRed);
		page.AddChild(CreateSettingRow("Show action log", _showActionLog));
		return page;
	}

	private static VBoxContainer CreateSettingsPage()
	{
		var page = new VBoxContainer
		{
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
			SizeFlagsVertical = SizeFlags.ExpandFill,
		};
		page.AddThemeConstantOverride("separation", 18);
		return page;
	}

	private static UiSelect CreateSelect() =>
		new(HudPalettes.PurpleRed) { SizeFlagsHorizontal = SizeFlags.ExpandFill };

	private static UiSlider CreateVolumeSlider()
		=> new(HudPalettes.PurpleRed) { SizeFlagsHorizontal = SizeFlags.ExpandFill };

	private static Control CreateSettingRow(string labelText, Control control)
	{
		var row = new HBoxContainer
		{
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
			CustomMinimumSize = new Vector2(0f, 44f),
		};
		row.AddThemeConstantOverride("separation", 20);
		row.AddChild(new Label
		{
			Text = labelText,
			CustomMinimumSize = new Vector2(260f, 0f),
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
			VerticalAlignment = VerticalAlignment.Center,
		});
		row.AddChild(control);
		return row;
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
		_baselineSnapshot = snapshot;
		_hasBaselineSnapshot = true;
		CallDeferred(MethodName.FitSettingsPanelToViewport);
	}

	private void OnSettingsTabChanged(long tab) =>
		_shell.SetBodyScrollingEnabled(tab != (int)SettingsTab.KeyBindings);

	private void OnEscapeRequested()
	{
		if (_unsavedChangesPopup.Visible)
			return;

		if (!IsDirty())
		{
			CloseRequested?.Invoke();
			return;
		}

		_unsavedChangesStatus.Text = "Apply your settings changes before exiting?";
		_shell.SetEscapeHandlingEnabled(false);
		_unsavedChangesPopup.Show();
	}

	private bool OnCloseRequested()
	{
		if (_unsavedChangesPopup.Visible)
			return false;

		if (IsDirty())
		{
			_unsavedChangesStatus.Text = "Apply your settings changes before exiting?";
			_shell.SetEscapeHandlingEnabled(false);
			_unsavedChangesPopup.Show();
			return false;
		}

		CloseRequested?.Invoke();
		return true;
	}

	private void ApplyAndExit()
	{
		var snapshot = CaptureUiSnapshot();
		if (!GameSettingsApplicator.TryCommit(snapshot, Host, out var error))
		{
			_unsavedChangesStatus.Text = error ?? "Could not apply settings.";
			return;
		}

		_baselineSnapshot = snapshot;
		_hasBaselineSnapshot = true;
		_unsavedChangesPopup.Hide();
		CloseRequested?.Invoke();
	}

	private void DiscardAndExit()
	{
		_unsavedChangesPopup.Hide();
		CloseRequested?.Invoke();
	}

	private bool IsDirty()
	{
		if (!_hasBaselineSnapshot)
			return false;

		var current = CaptureUiSnapshot();
		if (current.Video != _baselineSnapshot.Video
			|| current.Audio != _baselineSnapshot.Audio
			|| current.ShowTutorials != _baselineSnapshot.ShowTutorials
			|| current.MouseCameraPan != _baselineSnapshot.MouseCameraPan
			|| current.ShowActionLog != _baselineSnapshot.ShowActionLog
			|| current.KeyBindings.Count != _baselineSnapshot.KeyBindings.Count)
			return true;

		foreach (var (action, binding) in current.KeyBindings)
		{
			if (!_baselineSnapshot.KeyBindings.TryGetValue(action, out var baselineBinding)
				|| binding != baselineBinding)
				return true;
		}

		return false;
	}

	private void LoadSettingsToUi()
	{
		var video = GameSettings.ReadVideoConfig();
		_displayMode.Select(video.Mode == GameSettings.DisplayMode.Windowed ? 1 : 0);
		SelectResolution(video.Resolution);
		SelectUiScale(video.UiScale);
		UpdateResolutionAvailability();
		var audio = GameSettings.ReadAudioConfig();
		_masterVolume.Value = audio.MasterVolume * 100f;
		_musicVolume.Value = audio.MusicVolume * 100f;
		_sfxVolume.Value = audio.SfxVolume * 100f;
		_showTutorials.Value = GameSettings.ReadShowTutorials();
		_mouseCameraPan.Value = GameSettings.ReadMouseCameraPan();
		_showActionLog.Value = GameSettings.ReadShowActionLog();
	}

	private GameSettingsSessionSnapshot CaptureUiSnapshot() =>
		new(
			SelectedVideoConfig(),
			SelectedAudioConfig(),
			_showTutorials.Value,
			_mouseCameraPan.Value,
			_showActionLog.Value,
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
		_resolution.Select(GameSettings.TryFindResolutionIndex(
			resolution.X,
			resolution.Y,
			out var index)
			? index
			: 0);
	}

	private void SelectUiScale(float scale)
	{
		var normalized = GameSettings.NormalizeUiScale(scale);
		for (var i = 0; i < GameSettings.SupportedUiScales.Length; i++)
		{
			if (Mathf.IsEqualApprox(GameSettings.SupportedUiScales[i], normalized))
			{
				_uiScale.Select(i);
				return;
			}
		}

		_uiScale.Select(0);
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
				_showTutorials.Value = true;
				_mouseCameraPan.Value = true;
				_showActionLog.Value = true;
				break;
			case SettingsTab.KeyBindings:
				_keyBindingsTab.ResetToDefaults();
				break;
		}
	}

	private void ResetVideoUiToDefaults()
	{
		_displayMode.Select(0);
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
		var viewport = GetViewport().GetVisibleRect().Size;
		_shell.SetPanelMinimumSize(new Vector2(
			Mathf.Max(64f, Mathf.Min(880f, viewport.X - 40f)),
			Mathf.Max(64f, Mathf.Min(760f, viewport.Y - 40f))));
	}

	private void OnViewportSizeChanged() => FitSettingsPanelToViewport();
}
