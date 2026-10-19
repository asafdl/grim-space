using Godot;

namespace GrimSpace.Battle.Presentation.Ui;

internal static class TacticalActionButtonStyle
{
	private const string FramePath = "res://assets/ui/battle/tactical_action_frame.svg";
	private static Texture2D? _frame;
	private static readonly Color NormalModulate = new(0.72f, 0.74f, 0.8f);
	private static readonly Color HoverModulate = new(1.12f, 1.12f, 1.12f);
	private static readonly Color PressedModulate = new(0.62f, 0.66f, 0.74f);
	private static readonly Color DisabledModulate = new(0.4f, 0.42f, 0.46f, 0.55f);

	public static void Apply(Button button)
	{
		var frame = _frame ??= GD.Load<Texture2D>(FramePath)
			?? throw new InvalidOperationException($"Could not load tactical action frame '{FramePath}'.");

		ApplyInteractionStates(button, color => Frame(frame, color));
	}

	public static void ApplyInteractionStates(
		Button button,
		Func<Color, StyleBox> styleFor)
	{
		button.AddThemeStyleboxOverride("normal", styleFor(NormalModulate));
		button.AddThemeStyleboxOverride("hover", styleFor(HoverModulate));
		button.AddThemeStyleboxOverride("pressed", styleFor(PressedModulate));
		button.AddThemeStyleboxOverride("disabled", styleFor(DisabledModulate));
		button.AddThemeStyleboxOverride("focus", StyleBoxEmpty());

		button.AddThemeColorOverride("icon_normal_color", new Color(0.9f, 0.9f, 0.94f));
		button.AddThemeColorOverride("icon_hover_color", Colors.White);
		button.AddThemeColorOverride("icon_pressed_color", new Color(0.82f, 0.84f, 0.9f));
		button.AddThemeColorOverride("icon_disabled_color", new Color(0.5f, 0.52f, 0.56f, 0.5f));
		button.AddThemeColorOverride("font_color", new Color(0.9f, 0.9f, 0.94f));
		button.AddThemeColorOverride("font_hover_color", Colors.White);
		button.AddThemeColorOverride("font_pressed_color", new Color(0.82f, 0.84f, 0.9f));
		button.AddThemeColorOverride("font_disabled_color", new Color(0.5f, 0.52f, 0.56f, 0.5f));

		ApplyMotion(button);
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

	private static void ApplyMotion(Button button)
	{
		Tween? scaleTween = null;
		button.Resized += () => button.PivotOffset = button.Size * 0.5f;
		button.MouseEntered += () =>
		{
			if (button.Disabled)
				return;
			button.ZIndex = 2;
			scaleTween = AnimateScale(button, scaleTween, 1.06f, 0.07);
		};
		button.MouseExited += () =>
		{
			button.ZIndex = 0;
			scaleTween = AnimateScale(button, scaleTween, 1f, 0.09);
		};
		button.ButtonDown += () =>
		{
			if (!button.Disabled)
				scaleTween = AnimateScale(button, scaleTween, 0.97f, 0.045);
		};
		button.ButtonUp += () =>
		{
			if (!button.Disabled)
				scaleTween = AnimateScale(button, scaleTween, 1.06f, 0.06);
		};
	}

	private static Tween AnimateScale(
		Button button,
		Tween? current,
		float scale,
		double duration)
	{
		current?.Kill();
		var tween = button.CreateTween();
		tween.SetTrans(Tween.TransitionType.Quad);
		tween.SetEase(Tween.EaseType.Out);
		tween.TweenProperty(
			button,
			new NodePath("scale"),
			Vector2.One * scale,
			duration);
		return tween;
	}
}
