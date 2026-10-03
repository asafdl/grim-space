using Godot;
using GrimSpace.Components;
using GrimSpace.Presentation.Support;

namespace GrimSpace.Battle.Presentation.Ui;

public sealed partial class BattlePauseMenuOverlay : CanvasLayer
{
	public event Action? ContinueRequested;
	public event Action? SaveRequested;
	public event Action? RetireRequested;
	public event Action? RestartRequested;
	public event Action? MainMenuRequested;

	private Control _root = null!;
	private Button _saveButton = null!;
	private Button _restartButton = null!;
	private ReportIssueDialog _reportDialog = null!;

	public BattlePauseMenuOverlay()
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

	public void SetRestartEnabled(bool enabled) => _restartButton.Disabled = !enabled;

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
			Color = HudStyles.ModalBackdrop,
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

		var title = new Label
		{
			Text = BattleHudCopy.PauseMenuTitle,
			HorizontalAlignment = HorizontalAlignment.Center,
			ThemeTypeVariation = "OverlayTitle",
		};
		content.AddChild(title);

		content.AddChild(CreateMenuButton(
			BattleHudCopy.Continue,
			BattleHudCopy.ContinueTooltip,
			() => ContinueRequested?.Invoke()));
		_saveButton = CreateMenuButton(
			"Save",
			"Save the current battle",
			() => SaveRequested?.Invoke());
		content.AddChild(_saveButton);
		content.AddChild(CreateMenuButton(
			BattleHudCopy.Retire,
			BattleHudCopy.RetireTooltip,
			() => RetireRequested?.Invoke()));
		content.AddChild(CreateMenuButton(
			BattleHudCopy.Restart,
			BattleHudCopy.RestartTooltip,
			() => RestartRequested?.Invoke(),
			out _restartButton));
		content.AddChild(CreateMenuButton(
			BattleHudCopy.MainMenu,
			BattleHudCopy.MainMenuTooltip,
			() => MainMenuRequested?.Invoke()));

		_reportDialog = new ReportIssueDialog();
		_root.AddChild(_reportDialog);

		PauseMenuReportFooter.AppendMenuItem(content, () => _reportDialog.Open("battle"));
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (!Visible)
			return;

		if (_reportDialog.IsOpen)
			return;

		if (@event is InputEventKey { Pressed: true, Echo: false, Keycode: Key.Escape })
		{
			ContinueRequested?.Invoke();
			GetViewport().SetInputAsHandled();
		}
	}

	public void ShowSaveConfirmation() => _saveButton.Text = "✓ Saved";

	private static Button CreateMenuButton(string text, string tooltip, Action onPressed)
	{
		return CreateMenuButton(text, tooltip, onPressed, out _);
	}

	private static Button CreateMenuButton(
		string text,
		string tooltip,
		Action onPressed,
		out Button button)
	{
		button = new Button
		{
			Text = text,
			TooltipText = tooltip,
			CustomMinimumSize = new Vector2(220, 44),
			SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter,
		};
		HudStyles.StyleButton(button, HudActionKind.Secondary);
		button.Pressed += onPressed;
		return button;
	}
}
