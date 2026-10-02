using Godot;

namespace GrimSpace.Battle.Presentation.Ui;

public sealed partial class TurnFlowDivider : Control
{
	private const int BoxSize = 64;

	private readonly ColorRect _rule;
	private readonly Label _label;

	public TurnFlowDivider()
	{
		MouseFilter = MouseFilterEnum.Ignore;
		_rule = new ColorRect
		{
			Color = new Color(0.55f, 0.62f, 0.75f, 0.85f),
			MouseFilter = MouseFilterEnum.Ignore,
			Size = new Vector2(2, BoxSize + 8),
		};
		_label = new Label
		{
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Center,
			MouseFilter = MouseFilterEnum.Ignore,
		};
		_label.AddThemeFontSizeOverride("font_size", 11);
		_label.AddThemeColorOverride("font_color", new Color(0.65f, 0.72f, 0.82f, 0.9f));
		AddChild(_rule);
		AddChild(_label);
	}

	public void ShowAt(Vector2 position, int turnNumber)
	{
		Visible = true;
		Position = position;
		_rule.Position = new Vector2(0, 0);
		_label.Text = $"T{turnNumber}";
		_label.Position = new Vector2(-35f, -14f);
		_label.Size = new Vector2(32, 14);
	}

	public void HideDivider() => Visible = false;
}
