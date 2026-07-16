using Godot;

namespace GrimSpace.Battle.Presentation.Ui;

public sealed partial class BattleOutcomeOverlay : CanvasLayer
{
	public event Action? ResetRequested;

	private Control _root = null!;

	public BattleOutcomeOverlay()
	{
		Layer = 20;
		Build();
		Visible = false;
	}

	public new void SetVisible(bool visible) => Visible = visible;

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
			Color = new Color(0f, 0f, 0f, 0.55f),
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
			CustomMinimumSize = new Vector2(320, 180),
		};
		center.AddChild(panel);

		var column = new VBoxContainer
		{
			Alignment = BoxContainer.AlignmentMode.Center,
		};
		column.AddThemeConstantOverride("separation", 24);
		panel.AddChild(column);

		var margin = new MarginContainer();
		margin.AddThemeConstantOverride("margin_left", 32);
		margin.AddThemeConstantOverride("margin_right", 32);
		margin.AddThemeConstantOverride("margin_top", 28);
		margin.AddThemeConstantOverride("margin_bottom", 28);
		column.AddChild(margin);

		var content = new VBoxContainer
		{
			Alignment = BoxContainer.AlignmentMode.Center,
		};
		content.AddThemeConstantOverride("separation", 20);
		margin.AddChild(content);

		var title = new Label
		{
			Text = "You Win!",
			HorizontalAlignment = HorizontalAlignment.Center,
		};
		title.AddThemeFontSizeOverride("font_size", 36);
		content.AddChild(title);

		var resetButton = new Button
		{
			Text = "Reset",
			CustomMinimumSize = new Vector2(140, 44),
		};
		resetButton.Pressed += () => ResetRequested?.Invoke();
		content.AddChild(resetButton);
	}
}
