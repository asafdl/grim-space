using Godot;

namespace GrimSpace.Battle.Presentation.Ui;

public sealed partial class ActionInstructionBar : CenterContainer
{
	public event Action? ConfirmRequested;

	private Button _button = null!;

	public ActionInstructionBar()
	{
		CustomMinimumSize = new Vector2(0, 36);
		MouseFilter = MouseFilterEnum.Ignore;

		_button = new Button
		{
			FocusMode = FocusModeEnum.None,
			CustomMinimumSize = new Vector2(120, 36),
			MouseFilter = MouseFilterEnum.Ignore,
			Modulate = Colors.Transparent,
		};
		ApplyButtonStyles(_button);
		_button.Pressed += () =>
		{
			if (!_button.Disabled)
				ConfirmRequested?.Invoke();
		};
		AddChild(_button);
	}

	public void Apply(ActionInstruction instruction)
	{
		var show = instruction.Visible;
		_button.Modulate = show ? Colors.White : Colors.Transparent;
		_button.MouseFilter = show ? MouseFilterEnum.Stop : MouseFilterEnum.Ignore;
		if (!show)
			return;

		_button.Text = instruction.Label;
		_button.Disabled = !instruction.CanConfirm;
	}

	private static void ApplyButtonStyles(Button button)
	{
		var accent = new Color(0.55f, 0.78f, 1f);
		var normal = MakeStyle(new Color(0.14f, 0.18f, 0.26f, 1f), accent * new Color(1f, 1f, 1f, 0.65f), 2, 6);
		var hover = MakeStyle(new Color(0.2f, 0.26f, 0.36f, 1f), accent, 2, 6);
		var pressed = MakeStyle(new Color(0.24f, 0.32f, 0.44f, 1f), accent, 3, 6);
		var disabled = MakeStyle(new Color(0.1f, 0.11f, 0.13f, 0.9f), new Color(0.35f, 0.38f, 0.42f), 2, 6);

		button.AddThemeStyleboxOverride("normal", normal);
		button.AddThemeStyleboxOverride("hover", hover);
		button.AddThemeStyleboxOverride("pressed", pressed);
		button.AddThemeStyleboxOverride("disabled", disabled);
		button.AddThemeStyleboxOverride("focus", (StyleBox)normal.Duplicate());
		button.AddThemeColorOverride("font_color", new Color(0.92f, 0.96f, 1f));
		button.AddThemeColorOverride("font_hover_color", Colors.White);
		button.AddThemeColorOverride("font_pressed_color", Colors.White);
		button.AddThemeColorOverride("font_disabled_color", new Color(0.65f, 0.7f, 0.75f));
		button.AddThemeFontSizeOverride("font_size", 13);
	}

	private static StyleBoxFlat MakeStyle(Color bg, Color border, int borderWidth, int radius) =>
		new()
		{
			BgColor = bg,
			BorderColor = border,
			BorderWidthLeft = borderWidth,
			BorderWidthTop = borderWidth,
			BorderWidthRight = borderWidth,
			BorderWidthBottom = borderWidth,
			CornerRadiusTopLeft = radius,
			CornerRadiusTopRight = radius,
			CornerRadiusBottomRight = radius,
			CornerRadiusBottomLeft = radius,
			ContentMarginLeft = 10,
			ContentMarginRight = 10,
			ContentMarginTop = 4,
			ContentMarginBottom = 4,
		};
}
