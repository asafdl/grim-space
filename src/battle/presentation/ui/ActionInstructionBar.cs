using Godot;

namespace GrimSpace.Battle.Presentation.Ui;

public sealed partial class ActionInstructionBar : CenterContainer
{
	private readonly PanelContainer _panel;
	private readonly Label _label;

	public ActionInstructionBar()
	{
		CustomMinimumSize = new Vector2(0, 36);
		MouseFilter = MouseFilterEnum.Ignore;

		_panel = new PanelContainer
		{
			CustomMinimumSize = new Vector2(120, 36),
			MouseFilter = MouseFilterEnum.Ignore,
			Visible = false,
		};
		_panel.AddThemeStyleboxOverride("panel", MakeStyle());
		_label = new Label
		{
			MouseFilter = MouseFilterEnum.Ignore,
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Center,
			ThemeTypeVariation = "BattleInstruction",
		};
		_panel.AddChild(_label);
		AddChild(_panel);
	}

	public void Apply(ActionInstruction instruction)
	{
		_panel.Visible = instruction.Visible;
		if (!instruction.Visible)
			return;

		_label.Text = instruction.Label;
	}

	private static StyleBoxFlat MakeStyle() =>
		new()
		{
			BgColor = new Color(0.1f, 0.13f, 0.18f, 0.86f),
			BorderColor = new Color(0.55f, 0.78f, 1f, 0.55f),
			BorderWidthLeft = 1,
			BorderWidthTop = 1,
			BorderWidthRight = 1,
			BorderWidthBottom = 1,
			CornerRadiusTopLeft = 6,
			CornerRadiusTopRight = 6,
			CornerRadiusBottomRight = 6,
			CornerRadiusBottomLeft = 6,
			ContentMarginLeft = 10,
			ContentMarginRight = 10,
			ContentMarginTop = 4,
			ContentMarginBottom = 4,
		};
}
