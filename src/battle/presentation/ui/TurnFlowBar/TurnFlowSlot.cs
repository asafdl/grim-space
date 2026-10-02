using Godot;

namespace GrimSpace.Battle.Presentation.Ui;

public sealed partial class TurnFlowSlot : Button
{
	private const int BoxSize = 64;
	private const int CursorWidth = 20;
	private const int CursorHeight = 16;
	private const string ActiveCursorPath = "res://assets/ui/turn_flow/active_cursor.svg";

	private string? _unitId;
	private readonly TextureRect _activeCursor;

	public event Action<string>? UnitClicked;

	public TurnFlowSlot()
	{
		CustomMinimumSize = new Vector2(BoxSize, BoxSize);
		FocusMode = FocusModeEnum.None;
		MouseFilter = MouseFilterEnum.Stop;
		TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis;
		AddThemeStyleboxOverride("normal", MakeBoxStyle(active: false));
		AddThemeStyleboxOverride("hover", MakeBoxStyle(active: false, hover: true));
		AddThemeStyleboxOverride("disabled", MakeBoxStyle(active: false, dim: true));
		_activeCursor = new TextureRect
		{
			Texture = ResourceLoader.Load<Texture2D>(ActiveCursorPath),
			CustomMinimumSize = new Vector2(CursorWidth, CursorHeight),
			Size = new Vector2(CursorWidth, CursorHeight),
			Position = new Vector2((BoxSize - CursorWidth) * 0.5f, BoxSize - 2),
			MouseFilter = MouseFilterEnum.Ignore,
			MouseDefaultCursorShape = CursorShape.Arrow,
			Visible = false,
			ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
			StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
		};
		AddChild(_activeCursor);
		Pressed += OnPressed;
	}

	public void ShowUnit(string unitId, Texture2D? portrait, bool active, float opacity)
	{
		_unitId = unitId;
		Icon = portrait;
		Text = portrait is null ? unitId : string.Empty;
		Disabled = false;
		AddThemeStyleboxOverride("normal", MakeBoxStyle(active));
		_activeCursor.Visible = active;
		Modulate = active ? Colors.White : new Color(1f, 1f, 1f, opacity);
	}

	public void ShowEmpty()
	{
		_unitId = null;
		Icon = null;
		Text = string.Empty;
		Disabled = true;
		AddThemeStyleboxOverride("normal", MakeBoxStyle(active: false, dim: true));
		_activeCursor.Visible = false;
		Modulate = Colors.White;
	}

	private void OnPressed()
	{
		if (_unitId is not null)
			UnitClicked?.Invoke(_unitId);
	}

	private static StyleBoxFlat MakeBoxStyle(bool active, bool hover = false, bool dim = false)
	{
		if (dim)
			return MakeStyle(
				new Color(0.08f, 0.1f, 0.14f, 0f),
				new Color(0.25f, 0.32f, 0.45f, 0.35f),
				1);

		if (active)
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
