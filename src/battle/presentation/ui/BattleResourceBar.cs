using Godot;

namespace GrimSpace.Battle.Presentation.Ui;

public sealed partial class BattleResourceBar : HBoxContainer
{
	private readonly Label _label;
	private readonly ProgressBar _bar;
	private readonly Control _dividerHost;
	private readonly Label _valueLabel;
	private readonly List<ColorRect> _dividers = [];
	private int _maximum;

	public BattleResourceBar(string name, Color color, string tooltip)
	{
		CustomMinimumSize = new Vector2(154f, 16f);
		MouseFilter = MouseFilterEnum.Stop;
		TooltipText = tooltip;
		AddThemeConstantOverride("separation", 8);

		_label = new Label
		{
			Text = name,
			CustomMinimumSize = new Vector2(30f, 16f),
			MouseFilter = MouseFilterEnum.Ignore,
			VerticalAlignment = VerticalAlignment.Center,
			SizeFlagsHorizontal = SizeFlags.ShrinkBegin,
			ThemeTypeVariation = "BattleAp",
		};
		_label.AddThemeFontSizeOverride("font_size", 13);
		_label.AddThemeColorOverride("font_color", color.Lightened(0.18f));
		AddChild(_label);

		var barHost = new Control
		{
			CustomMinimumSize = new Vector2(116f, 16f),
			MouseFilter = MouseFilterEnum.Ignore,
			SizeFlagsHorizontal = SizeFlags.ShrinkEnd,
			SizeFlagsVertical = SizeFlags.ShrinkCenter,
		};
		AddChild(barHost);

		_bar = new ProgressBar
		{
			AnchorsPreset = (int)Control.LayoutPreset.FullRect,
			AnchorRight = 1f,
			AnchorBottom = 1f,
			GrowHorizontal = GrowDirection.Both,
			GrowVertical = GrowDirection.Both,
			MouseFilter = MouseFilterEnum.Ignore,
			ShowPercentage = false,
		};
		_bar.AddThemeStyleboxOverride("background", BarStyle(color.Darkened(0.78f), color.Darkened(0.45f)));
		_bar.AddThemeStyleboxOverride("fill", BarStyle(color, color.Lightened(0.18f)));
		barHost.AddChild(_bar);

		_dividerHost = new Control
		{
			AnchorsPreset = (int)Control.LayoutPreset.FullRect,
			AnchorRight = 1f,
			AnchorBottom = 1f,
			GrowHorizontal = GrowDirection.Both,
			GrowVertical = GrowDirection.Both,
			MouseFilter = MouseFilterEnum.Ignore,
		};
		barHost.AddChild(_dividerHost);

		_valueLabel = new Label
		{
			AnchorsPreset = (int)Control.LayoutPreset.FullRect,
			AnchorRight = 1f,
			AnchorBottom = 1f,
			OffsetRight = -3f,
			OffsetBottom = -1f,
			GrowHorizontal = GrowDirection.Both,
			GrowVertical = GrowDirection.Both,
			MouseFilter = MouseFilterEnum.Ignore,
			HorizontalAlignment = HorizontalAlignment.Right,
			VerticalAlignment = VerticalAlignment.Bottom,
			Text = "0/0",
		};
		_valueLabel.AddThemeFontSizeOverride("font_size", 9);
		_valueLabel.AddThemeColorOverride("font_color", Colors.White);
		_valueLabel.AddThemeColorOverride("font_outline_color", new Color(0.02f, 0.02f, 0.03f, 0.9f));
		_valueLabel.AddThemeConstantOverride("outline_size", 1);
		barHost.AddChild(_valueLabel);
	}

	public void Apply(int current, int max)
	{
		_valueLabel.Text = $"{current}/{max}";
		_bar.MaxValue = max;
		_bar.Value = current;
		if (_maximum == max)
			return;

		_maximum = max;
		foreach (var divider in _dividers)
			divider.QueueFree();
		_dividers.Clear();
		for (var i = 1; i < max; i++)
		{
			var anchor = i / (float)max;
			var divider = new ColorRect
			{
				AnchorLeft = anchor,
				AnchorTop = 0f,
				AnchorRight = anchor,
				AnchorBottom = 1f,
				OffsetLeft = -0.5f,
				OffsetTop = 3f,
				OffsetRight = 0.5f,
				OffsetBottom = -3f,
				GrowHorizontal = GrowDirection.Both,
				GrowVertical = GrowDirection.Both,
				MouseFilter = MouseFilterEnum.Ignore,
				Color = new Color(0.92f, 0.9f, 0.95f, 0.42f),
			};
			_dividerHost.AddChild(divider);
			_dividers.Add(divider);
		}
	}

	private static StyleBoxFlat BarStyle(Color fill, Color border) =>
		new()
		{
			BgColor = fill,
			BorderColor = border,
			BorderWidthLeft = 1,
			BorderWidthTop = 1,
			BorderWidthRight = 1,
			BorderWidthBottom = 1,
			CornerRadiusTopLeft = 1,
			CornerRadiusTopRight = 1,
			CornerRadiusBottomRight = 1,
			CornerRadiusBottomLeft = 1,
		};
}
