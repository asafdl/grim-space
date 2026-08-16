using Godot;

namespace GrimSpace.Battle.Presentation.Ui;

public sealed partial class BattlePauseMenuOverlay : CanvasLayer
{
	public event Action? ContinueRequested;
	public event Action? RetireRequested;
	public event Action? RestartRequested;
	public event Action? MainMenuRequested;

	public BattlePauseMenuOverlay()
	{
		Layer = 15;
		Build();
		Visible = false;
	}

	private void Build()
	{
		var root = new Control
		{
			AnchorsPreset = (int)Control.LayoutPreset.FullRect,
			AnchorRight = 1f,
			AnchorBottom = 1f,
			GrowHorizontal = Control.GrowDirection.Both,
			GrowVertical = Control.GrowDirection.Both,
			MouseFilter = Control.MouseFilterEnum.Stop,
		};
		AddChild(root);

		var backdrop = new ColorRect
		{
			AnchorsPreset = (int)Control.LayoutPreset.FullRect,
			AnchorRight = 1f,
			AnchorBottom = 1f,
			GrowHorizontal = Control.GrowDirection.Both,
			GrowVertical = Control.GrowDirection.Both,
			Color = new Color(0f, 0f, 0f, 0.55f),
		};
		root.AddChild(backdrop);

		var center = new CenterContainer
		{
			AnchorsPreset = (int)Control.LayoutPreset.FullRect,
			AnchorRight = 1f,
			AnchorBottom = 1f,
			GrowHorizontal = Control.GrowDirection.Both,
			GrowVertical = Control.GrowDirection.Both,
		};
		root.AddChild(center);

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
		};
		title.AddThemeFontSizeOverride("font_size", 28);
		content.AddChild(title);

		content.AddChild(CreateMenuButton(
			BattleHudCopy.Continue,
			BattleHudCopy.ContinueTooltip,
			() => ContinueRequested?.Invoke()));
		content.AddChild(CreateMenuButton(
			BattleHudCopy.Retire,
			BattleHudCopy.RetireTooltip,
			() => RetireRequested?.Invoke()));
		content.AddChild(CreateMenuButton(
			BattleHudCopy.Restart,
			BattleHudCopy.RestartTooltip,
			() => RestartRequested?.Invoke()));
		content.AddChild(CreateMenuButton(
			BattleHudCopy.MainMenu,
			BattleHudCopy.MainMenuTooltip,
			() => MainMenuRequested?.Invoke()));
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (!Visible)
			return;

		if (@event is InputEventKey { Pressed: true, Echo: false, Keycode: Key.Escape })
		{
			ContinueRequested?.Invoke();
			GetViewport().SetInputAsHandled();
		}
	}

	private static Button CreateMenuButton(string text, string tooltip, Action onPressed)
	{
		var button = new Button
		{
			Text = text,
			TooltipText = tooltip,
			CustomMinimumSize = new Vector2(220, 44),
			SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter,
		};
		button.Pressed += onPressed;
		return button;
	}
}
