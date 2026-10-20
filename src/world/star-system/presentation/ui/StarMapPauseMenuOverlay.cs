using Godot;
using GrimSpace.Components;
using GrimSpace.Presentation.Menu;
using GrimSpace.Presentation.Support;

namespace GrimSpace.World.StarSystem.Presentation.Ui;

public sealed partial class StarMapPauseMenuOverlay : CanvasLayer
{
	public event Action? ContinueRequested;
	public event Action? SaveRequested;
	public event Action? MainMenuRequested;

	private Control _root = null!;
	private Button _saveButton = null!;
	private ReportIssueDialog _reportDialog = null!;
	private readonly PauseMenuSettingsHost _settings = new();

	public bool IsSettingsOpen => _settings.IsOpen;

	public StarMapPauseMenuOverlay()
	{
		Layer = 15;
		Build();
		Visible = false;
	}

	public bool IsReportDialogOpen => _reportDialog.IsOpen;

	public void ApplyTheme(Theme theme)
	{
		_root.Theme = theme;
		_reportDialog.ApplyTheme(theme);
	}

	private void Build()
	{
		_root = new Control
		{
			AnchorsPreset = (int)Control.LayoutPreset.FullRect,
			AnchorRight = 1f,
			AnchorBottom = 1f,
			GrowHorizontal = Control.GrowDirection.Both,
			GrowVertical = Control.GrowDirection.Both,
			MouseFilter = Control.MouseFilterEnum.Stop,
		};
		AddChild(_root);

		var backdrop = new ColorRect
		{
			AnchorsPreset = (int)Control.LayoutPreset.FullRect,
			AnchorRight = 1f,
			AnchorBottom = 1f,
			GrowHorizontal = Control.GrowDirection.Both,
			GrowVertical = Control.GrowDirection.Both,
			Color = MenuUi.ModalBackdropColor,
		};
		_root.AddChild(backdrop);

		var center = new CenterContainer
		{
			AnchorsPreset = (int)Control.LayoutPreset.FullRect,
			AnchorRight = 1f,
			AnchorBottom = 1f,
			GrowHorizontal = Control.GrowDirection.Both,
			GrowVertical = Control.GrowDirection.Both,
		};
		_root.AddChild(center);

		var panel = new PanelContainer
		{
			CustomMinimumSize = new Vector2(320, 0),
		};
		center.AddChild(panel);

		var margin = new MarginContainer();
		margin.AddThemeConstantOverride("margin_left", 28);
		margin.AddThemeConstantOverride("margin_right", 28);
		margin.AddThemeConstantOverride("margin_top", 24);
		margin.AddThemeConstantOverride("margin_bottom", 24);
		panel.AddChild(margin);

		var content = new VBoxContainer
		{
			Alignment = BoxContainer.AlignmentMode.Center,
		};
		content.AddThemeConstantOverride("separation", 12);
		margin.AddChild(content);

		content.AddChild(new Label
		{
			Text = "Paused",
			HorizontalAlignment = HorizontalAlignment.Center,
			ThemeTypeVariation = "OverlayTitle",
		});

		content.AddChild(CreateMenuButton("Continue", () => ContinueRequested?.Invoke()));
		_saveButton = CreateMenuButton("Save", () => SaveRequested?.Invoke());
		content.AddChild(_saveButton);
		content.AddChild(CreateMenuButton("Settings", () => _settings.Open()));
		content.AddChild(CreateMenuButton("Save and Quit", () => MainMenuRequested?.Invoke()));

		_reportDialog = new ReportIssueDialog();
		_root.AddChild(_reportDialog);
		_settings.Attach(_root);

		PauseMenuReportFooter.AppendMenuItem(content, () => _reportDialog.Open("star-map"));
	}

	public void ShowSaveConfirmation() => _saveButton.Text = "✓ Saved";

	public bool TryHandleInput(InputEvent @event)
	{
		if (!Visible && !_reportDialog.IsOpen && !_settings.IsOpen)
			return false;

		if (_reportDialog.IsOpen)
			return true;

		if (_settings.TryCloseOnEscape(@event))
			return true;

		if (_settings.IsOpen)
			return true;

		if (@event is InputEventKey { Pressed: true, Echo: false, Keycode: Key.Escape })
		{
			ContinueRequested?.Invoke();
			GetViewport().SetInputAsHandled();
			return true;
		}

		GetViewport().SetInputAsHandled();
		return true;
	}

	private static Button CreateMenuButton(string text, Action onPressed)
	{
		var button = new Button
		{
			Text = text,
			CustomMinimumSize = new Vector2(220, 44),
			SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter,
		};
		HudStyles.StyleMenuNavButton(button);
		button.Pressed += onPressed;
		return button;
	}
}
