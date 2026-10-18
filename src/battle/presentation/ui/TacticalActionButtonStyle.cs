using Godot;

namespace GrimSpace.Battle.Presentation.Ui;

internal static class TacticalActionButtonStyle
{
	private const string FramePath = "res://assets/ui/battle/tactical_action_frame.svg";
	private static Texture2D? _frame;

	public static void Apply(Button button, Color accent)
	{
		var frame = _frame ??= GD.Load<Texture2D>(FramePath)
			?? throw new InvalidOperationException($"Could not load tactical action frame '{FramePath}'.");

		button.AddThemeStyleboxOverride("normal", Frame(frame, Colors.White));
		button.AddThemeStyleboxOverride("hover", Frame(frame, accent.Lightened(0.22f)));
		button.AddThemeStyleboxOverride("pressed", Frame(frame, accent));
		button.AddThemeStyleboxOverride(
			"disabled",
			Frame(frame, new Color(0.42f, 0.46f, 0.52f, 0.58f)));
		button.AddThemeStyleboxOverride("focus", StyleBoxEmpty());
		button.AddThemeColorOverride("icon_disabled_color", new Color(0.55f, 0.58f, 0.62f, 0.55f));
	}

	public static Label AddHotkey(Button button, string text)
	{
		var label = new Label
		{
			MouseFilter = Control.MouseFilterEnum.Ignore,
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Center,
			AnchorLeft = 1f,
			AnchorTop = 1f,
			AnchorRight = 1f,
			AnchorBottom = 1f,
			OffsetLeft = -26f,
			OffsetTop = -26f,
			OffsetRight = 0f,
			OffsetBottom = 0f,
			GrowHorizontal = Control.GrowDirection.Begin,
			GrowVertical = Control.GrowDirection.Begin,
			ClipText = true,
		};
		label.AddThemeColorOverride("font_color", new Color(0.9f, 0.96f, 1f));
		label.AddThemeColorOverride("font_outline_color", new Color(0.02f, 0.05f, 0.08f));
		label.AddThemeConstantOverride("outline_size", 1);
		SetHotkey(label, text);
		button.AddChild(label);
		return label;
	}

	public static void SetHotkey(Label label, string text)
	{
		label.Text = text;
		label.AddThemeFontSizeOverride(
			"font_size",
			text.Length switch
			{
				<= 1 => 14,
				<= 3 => 10,
				<= 5 => 8,
				_ => 7,
			});
	}

	private static StyleBoxTexture Frame(Texture2D texture, Color modulate) =>
		new()
		{
			Texture = texture,
			ModulateColor = modulate,
			ContentMarginLeft = 5,
			ContentMarginTop = 5,
			ContentMarginRight = 5,
			ContentMarginBottom = 5,
		};

	private static StyleBoxEmpty StyleBoxEmpty() =>
		new()
		{
			ContentMarginLeft = 5,
			ContentMarginTop = 5,
			ContentMarginRight = 5,
			ContentMarginBottom = 5,
		};
}
