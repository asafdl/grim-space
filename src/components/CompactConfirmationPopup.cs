using Godot;

namespace GrimSpace.Components;

public sealed partial class CompactConfirmationPopup : PopupPanel
{
	private const int PopupWidth = 88;
	private const int PopupHeight = 36;
	private Action? _confirmed;

	public CompactConfirmationPopup()
	{
		Exclusive = false;
		Unresizable = true;
		PopupHide += () => _confirmed = null;

		var margin = new MarginContainer();
		margin.AddThemeConstantOverride("margin_left", 6);
		margin.AddThemeConstantOverride("margin_right", 6);
		margin.AddThemeConstantOverride("margin_top", 4);
		margin.AddThemeConstantOverride("margin_bottom", 4);
		AddChild(margin);

		var content = new HBoxContainer
		{
			MouseFilter = Control.MouseFilterEnum.Stop,
		};
		content.AddThemeConstantOverride("separation", 4);
		margin.AddChild(content);

		var prompt = new Label
		{
			Text = "Confirm:",
			VerticalAlignment = VerticalAlignment.Center,
			MouseFilter = Control.MouseFilterEnum.Ignore,
		};
		HudStyles.ApplyTextRole(prompt, HudTextRole.Body);
		prompt.AddThemeFontSizeOverride("font_size", 14);
		content.AddChild(prompt);

		var confirm = new Button
		{
			Text = "✓",
			CustomMinimumSize = new Vector2(24, 24),
			FocusMode = Control.FocusModeEnum.All,
			ThemeTypeVariation = "MapIconButton",
		};
		confirm.AddThemeFontSizeOverride("font_size", 13);
		confirm.AddThemeColorOverride("font_color", HudStyles.AccentCyan);
		confirm.AddThemeColorOverride("font_hover_color", Colors.White);
		confirm.AddThemeColorOverride("font_pressed_color", Colors.White);
		confirm.Pressed += Confirm;
		content.AddChild(confirm);
	}

	public void Request(Control anchor, Action confirmed)
	{
		_confirmed = confirmed;
		var anchorRect = anchor.GetGlobalRect();
		var position = new Vector2I(
			Mathf.RoundToInt(anchorRect.End.X - PopupWidth),
			Mathf.RoundToInt(anchorRect.End.Y + 4));
		Popup(new Rect2I(position, new Vector2I(PopupWidth, PopupHeight)));
	}

	private void Confirm()
	{
		if (_confirmed is not { } confirmed)
			return;

		_confirmed = null;
		Hide();
		confirmed();
	}
}
