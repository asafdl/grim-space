using Godot;
using GrimSpace.Application;
using GrimSpace.Components;

namespace GrimSpace.Battle.Presentation.Ui;

public sealed partial class ManeuverBar : PanelContainer
{
	public event Action? MoveModeRequested;

	private const int SlotSize = 64;
	private const float IconPx = 40f;
	private static readonly Color Accent = new(0.55f, 0.78f, 1f);

	private readonly ButtonGroup _modeGroup;
	private Button _moveButton = null!;
	private Label _apLabel = null!;
	private Label _moveHotkeyLabel = null!;

	public ManeuverBar(ButtonGroup modeGroup)
	{
		_modeGroup = modeGroup;
		MouseFilter = MouseFilterEnum.Stop;
		AddThemeStyleboxOverride("panel", MakeStyle(
			new Color(0.08f, 0.1f, 0.14f, 0.92f),
			new Color(0.35f, 0.55f, 0.85f, 0.75f),
			2,
			8));
		Build();
	}

	public void SetMode(EPlayerMode mode)
	{
		_moveButton.SetBlockSignals(true);
		_moveButton.ButtonPressed = mode == EPlayerMode.Move;
		_moveButton.SetBlockSignals(false);
	}

	public void Configure(bool canAct, int apCurrent, int apMax)
	{
		_moveButton.Disabled = !canAct;
		_apLabel.Text = BattleHudCopy.Charges(apCurrent, apMax);
	}

	public bool TryActivateMove()
	{
		if (_moveButton.Disabled)
			return false;
		_moveButton.ButtonPressed = true;
		return true;
	}

	public void RefreshBindingLabels() =>
		_moveHotkeyLabel.Text = GameInputBindings.Label("battle_move_mode");

	private void Build()
	{
		var pad = new MarginContainer();
		pad.AddThemeConstantOverride("margin_left", 8);
		pad.AddThemeConstantOverride("margin_right", 8);
		pad.AddThemeConstantOverride("margin_top", 8);
		pad.AddThemeConstantOverride("margin_bottom", 8);
		AddChild(pad);

		var col = new VBoxContainer();
		col.AddThemeConstantOverride("separation", 6);
		pad.AddChild(col);

		_apLabel = new Label
		{
			Text = "0/0",
			MouseFilter = MouseFilterEnum.Ignore,
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Center,
			ThemeTypeVariation = "BattleAp",
		};
		col.AddChild(_apLabel);

		_moveButton = new Button
		{
			CustomMinimumSize = new Vector2(SlotSize, SlotSize),
			Icon = SvgIconLoader.Load("res://assets/ui/abilities/move.svg", Accent, (int)IconPx),
			ExpandIcon = false,
			IconAlignment = HorizontalAlignment.Center,
			VerticalIconAlignment = VerticalAlignment.Center,
			Alignment = HorizontalAlignment.Center,
			TooltipText = BattleHudCopy.MoveTooltip,
			FocusMode = FocusModeEnum.None,
			ToggleMode = true,
			ButtonGroup = _modeGroup,
			ButtonPressed = true,
		};
		_moveButton.Toggled += pressed =>
		{
			if (pressed)
				MoveModeRequested?.Invoke();
		};
		_moveHotkeyLabel = AddHotkeyBadge(_moveButton, GameInputBindings.Label("battle_move_mode"));
		col.AddChild(_moveButton);
	}

	private static Label AddHotkeyBadge(Button button, string text)
	{
		var width = text.Length <= 1 ? 16f : 8f + text.Length * 6.5f;
		const float height = 14f;
		var keycap = new PanelContainer
		{
			MouseFilter = MouseFilterEnum.Ignore,
			CustomMinimumSize = new Vector2(width, height),
			AnchorLeft = 0.5f,
			AnchorTop = 0f,
			AnchorRight = 0.5f,
			AnchorBottom = 0f,
			OffsetLeft = -width * 0.5f,
			OffsetTop = -height * 0.5f,
			OffsetRight = width * 0.5f,
			OffsetBottom = height * 0.5f,
			GrowHorizontal = GrowDirection.Both,
		};
		keycap.AddThemeStyleboxOverride("panel", new StyleBoxFlat
		{
			BgColor = new Color(0.06f, 0.08f, 0.12f, 0.95f),
			BorderColor = new Color(0.7f, 0.82f, 1f, 0.85f),
			BorderWidthLeft = 1,
			BorderWidthTop = 1,
			BorderWidthRight = 1,
			BorderWidthBottom = 1,
			CornerRadiusTopLeft = 2,
			CornerRadiusTopRight = 2,
			CornerRadiusBottomRight = 2,
			CornerRadiusBottomLeft = 2,
			ContentMarginLeft = 3,
			ContentMarginRight = 3,
		});

		var label = new Label
		{
			Text = text,
			MouseFilter = MouseFilterEnum.Ignore,
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Center,
			ThemeTypeVariation = "Micro",
		};
		keycap.AddChild(label);
		button.AddChild(keycap);
		return label;
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
			ContentMarginLeft = 4,
			ContentMarginRight = 4,
			ContentMarginTop = 4,
			ContentMarginBottom = 4,
		};
}
