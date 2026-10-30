using Godot;

namespace GrimSpace.Components;

internal sealed partial class CenteredCarouselSlot : Button
{
	private const int CursorWidth = 20;
	private const int CursorHeight = 16;
	private const string HighlightCursorPath = "res://assets/ui/turn_flow/active_cursor.svg";

	private string? _itemId;
	private readonly TextureRect _highlightCursor;

	public event Action<string>? ItemClicked;

	public CenteredCarouselSlot(int size)
	{
		CustomMinimumSize = new Vector2(size, size);
		FocusMode = FocusModeEnum.None;
		MouseFilter = MouseFilterEnum.Stop;
		TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis;
		AddThemeStyleboxOverride("normal", MakeBoxStyle(highlighted: false));
		AddThemeStyleboxOverride("hover", MakeBoxStyle(highlighted: false, hover: true));
		AddThemeStyleboxOverride("disabled", MakeBoxStyle(highlighted: false, dim: true));
		_highlightCursor = new TextureRect
		{
			Texture = ResourceLoader.Load<Texture2D>(HighlightCursorPath),
			CustomMinimumSize = new Vector2(CursorWidth, CursorHeight),
			Size = new Vector2(CursorWidth, CursorHeight),
			Position = new Vector2((size - CursorWidth) * 0.5f, size - 2),
			MouseFilter = MouseFilterEnum.Ignore,
			MouseDefaultCursorShape = CursorShape.Arrow,
			Visible = false,
			ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
			StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
		};
		AddChild(_highlightCursor);
		Pressed += OnPressed;
	}

	public void ShowItem(
		CenteredCarouselEntry item,
		Texture2D? icon,
		Color iconTint,
		bool highlighted,
		float opacity)
	{
		_itemId = item.Id;
		Icon = icon;
		Text = icon is null ? item.Label : string.Empty;
		Disabled = false;
		AddThemeStyleboxOverride("normal", MakeBoxStyle(highlighted));
		_highlightCursor.Visible = highlighted;
		ApplyIconTint(iconTint);
		Modulate = highlighted ? Colors.White : new Color(1f, 1f, 1f, opacity);
	}

	public void ShowEmpty()
	{
		_itemId = null;
		Icon = null;
		Text = string.Empty;
		Disabled = true;
		AddThemeStyleboxOverride("normal", MakeBoxStyle(highlighted: false, dim: true));
		_highlightCursor.Visible = false;
		ApplyIconTint(Colors.White);
		Modulate = Colors.White;
	}

	private void ApplyIconTint(Color tint)
	{
		AddThemeColorOverride("icon_normal_color", tint);
		AddThemeColorOverride("icon_hover_color", tint);
		AddThemeColorOverride("icon_pressed_color", tint);
		AddThemeColorOverride("icon_disabled_color", tint);
		AddThemeColorOverride("icon_focus_color", tint);
	}

	private void OnPressed()
	{
		if (_itemId is not null)
			ItemClicked?.Invoke(_itemId);
	}

	private static StyleBoxFlat MakeBoxStyle(bool highlighted, bool hover = false, bool dim = false)
	{
		if (dim)
			return MakeStyle(
				new Color(0.08f, 0.1f, 0.14f, 0f),
				new Color(0.25f, 0.32f, 0.45f, 0.35f),
				1);

		if (highlighted)
			return MakeStyle(
				new Color(0.18f, 0.12f, 0.2f, 0f),
				new Color(0.35f, 0.48f, 0.68f, 0.8f),
				1);

		return hover
			? MakeStyle(
				new Color(0.16f, 0.2f, 0.29f, 0f),
				new Color(0.65f, 0.8f, 1f, 1f),
				2)
			: MakeStyle(
				new Color(0.1f, 0.13f, 0.19f, 0f),
				new Color(0.35f, 0.48f, 0.68f, 0.8f),
				1);
	}

	private static StyleBoxFlat MakeStyle(Color background, Color border, int borderWidth) =>
		new()
		{
			BgColor = background,
			BorderColor = border,
			BorderWidthLeft = borderWidth,
			BorderWidthTop = borderWidth,
			BorderWidthRight = borderWidth,
			BorderWidthBottom = borderWidth,
			CornerRadiusTopLeft = 4,
			CornerRadiusTopRight = 4,
			CornerRadiusBottomRight = 4,
			CornerRadiusBottomLeft = 4,
			ContentMarginLeft = 6,
			ContentMarginRight = 6,
		};
}
