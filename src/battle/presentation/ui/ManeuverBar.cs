using Godot;
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
		col.AddChild(_moveButton);
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
