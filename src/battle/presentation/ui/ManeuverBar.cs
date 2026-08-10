using Godot;
using GrimSpace.Battle.Movement;
using GrimSpace.Battle.Movement.Enums;

namespace GrimSpace.Battle.Presentation.Ui;

/// <summary>Move + orientation controls with AP remaining/max.</summary>
public sealed partial class ManeuverBar : PanelContainer
{
	public event Action<EPlayerMode>? ModeChanged;
	public event Action<EHeadingTurn>? HeadingTurnRequested;
	public event Action<ERollDirection>? RollRequested;

	private const int SlotSize = 64;
	private const float IconPx = 40f;
	private const float SvgSourceSize = 512f;

	private static readonly Color Accent = new(0.55f, 0.78f, 1f);

	private readonly ButtonGroup _modeGroup;
	private Button _moveButton = null!;
	private Button _yawButton = null!;
	private Button _spinButton = null!;
	private Label _apLabel = null!;
	private Label _momentumLabel = null!;

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

	public void Configure(bool canAct, int apCurrent, int apMax, int momentum)
	{
		_moveButton.Disabled = !canAct;
		_yawButton.Disabled = !canAct;
		_spinButton.Disabled = !canAct;
		_apLabel.Text = BattleHudCopy.Charges(apCurrent, apMax);
		_momentumLabel.Text = BattleHudCopy.MomentumStat(momentum, MomentumConfig.MaxLevel);
	}

	public bool TryActivateMove()
	{
		if (_moveButton.Disabled)
			return false;
		_moveButton.ButtonPressed = true;
		return true;
	}

	public bool TryYaw()
	{
		if (_yawButton.Disabled)
			return false;
		HeadingTurnRequested?.Invoke(EHeadingTurn.YawRight);
		return true;
	}

	public bool TrySpin()
	{
		if (_spinButton.Disabled)
			return false;
		RollRequested?.Invoke(ERollDirection.Clockwise);
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

		var statRow = new HBoxContainer
		{
			Alignment = BoxContainer.AlignmentMode.Center,
			MouseFilter = MouseFilterEnum.Ignore,
		};
		statRow.AddThemeConstantOverride("separation", 10);
		col.AddChild(statRow);

		_apLabel = new Label
		{
			Text = "0/0",
			MouseFilter = MouseFilterEnum.Ignore,
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Center,
		};
		_apLabel.AddThemeFontSizeOverride("font_size", 14);
		_apLabel.AddThemeColorOverride("font_color", new Color(0.55f, 0.95f, 0.65f));
		statRow.AddChild(_apLabel);

		_momentumLabel = new Label
		{
			Text = "M0/0",
			MouseFilter = MouseFilterEnum.Stop,
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Center,
			TooltipText = BattleHudCopy.MomentumTooltip,
		};
		_momentumLabel.AddThemeFontSizeOverride("font_size", 12);
		_momentumLabel.AddThemeColorOverride("font_color", new Color(0.75f, 0.82f, 1f));
		statRow.AddChild(_momentumLabel);

		var row = new HBoxContainer();
		row.AddThemeConstantOverride("separation", 8);
		col.AddChild(row);

		_moveButton = CreateModeSlot(
			hotkey: "1",
			tooltip: BattleHudCopy.MoveTooltip,
			iconPath: "res://assets/ui/abilities/move.svg",
			onPressed: () => ModeChanged?.Invoke(EPlayerMode.Move));
		row.AddChild(_moveButton);

		var orientCol = new VBoxContainer();
		orientCol.AddThemeConstantOverride("separation", 8);
		row.AddChild(orientCol);

		_yawButton = CreateActionSlot(
			hotkey: "E",
			tooltip: BattleHudCopy.YawTooltip,
			iconPath: "res://assets/ui/abilities/yaw.svg",
			onPressed: () => HeadingTurnRequested?.Invoke(EHeadingTurn.YawRight));
		_spinButton = CreateActionSlot(
			hotkey: "Q",
			tooltip: BattleHudCopy.SpinTooltip,
			iconPath: "res://assets/ui/abilities/spin.svg",
			onPressed: () => RollRequested?.Invoke(ERollDirection.Clockwise));

		orientCol.AddChild(_yawButton);
		orientCol.AddChild(_spinButton);

		_moveButton.ButtonPressed = true;
	}

	private Button CreateModeSlot(string hotkey, string tooltip, string iconPath, Action onPressed)
	{
		var button = CreateSlotBase(hotkey, tooltip, iconPath);
		button.ToggleMode = true;
		button.ButtonGroup = _modeGroup;
		button.Toggled += pressed =>
		{
			if (pressed)
				onPressed();
		};
		return button;
	}

	private static Button CreateActionSlot(string hotkey, string tooltip, string iconPath, Action onPressed)
	{
		var button = CreateSlotBase(hotkey, tooltip, iconPath);
		button.Pressed += onPressed;
		return button;
	}

	private static Button CreateSlotBase(string hotkey, string tooltip, string iconPath)
	{
		var button = new Button
		{
			CustomMinimumSize = new Vector2(SlotSize, SlotSize),
			Icon = LoadSvgIcon(iconPath),
			ExpandIcon = false,
			IconAlignment = HorizontalAlignment.Center,
			VerticalIconAlignment = VerticalAlignment.Center,
			Alignment = HorizontalAlignment.Center,
			TooltipText = tooltip,
			FocusMode = FocusModeEnum.None,
			ClipContents = false,
		};
		button.AddThemeConstantOverride("h_separation", 0);
		ApplySlotStyles(button);
		AddHotkeyBadge(button, hotkey);
		return button;
	}

	private static void ApplySlotStyles(Button button)
	{
		var normal = MakeStyle(new Color(0.12f, 0.14f, 0.2f, 1f), Accent * new Color(1f, 1f, 1f, 0.55f), 2, 4);
		var hover = MakeStyle(new Color(0.18f, 0.22f, 0.3f, 1f), Accent, 2, 4);
		var pressed = MakeStyle(new Color(0.22f, 0.28f, 0.4f, 1f), Accent, 3, 4);
		var disabled = MakeStyle(new Color(0.08f, 0.09f, 0.11f, 0.85f), new Color(0.25f, 0.28f, 0.32f), 2, 4);

		button.AddThemeStyleboxOverride("normal", normal);
		button.AddThemeStyleboxOverride("hover", hover);
		button.AddThemeStyleboxOverride("pressed", pressed);
		button.AddThemeStyleboxOverride("disabled", disabled);
		button.AddThemeStyleboxOverride("focus", (StyleBox)pressed.Duplicate());
	}

	private static void AddHotkeyBadge(Button button, string text)
	{
		const float width = 16f;
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
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
			SizeFlagsVertical = SizeFlags.ExpandFill,
		};
		label.AddThemeFontSizeOverride("font_size", 10);
		label.AddThemeColorOverride("font_color", new Color(0.9f, 0.95f, 1f));
		keycap.AddChild(label);
		button.AddChild(keycap);
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

	private static Texture2D LoadSvgIcon(string path)
	{
		if (!Godot.FileAccess.FileExists(path))
			return ImageTexture.CreateFromImage(Image.CreateEmpty((int)IconPx, (int)IconPx, false, Image.Format.Rgba8));

		var svg = Godot.FileAccess.GetFileAsString(path);
		var image = new Image();
		var err = image.LoadSvgFromString(svg, IconPx / SvgSourceSize);
		if (err != Error.Ok)
			return ImageTexture.CreateFromImage(Image.CreateEmpty((int)IconPx, (int)IconPx, false, Image.Format.Rgba8));

		var w = image.GetWidth();
		var h = image.GetHeight();
		for (var y = 0; y < h; y++)
		for (var x = 0; x < w; x++)
		{
			var p = image.GetPixel(x, y);
			if (p.A <= 0.001f)
				continue;
			image.SetPixel(x, y, new Color(Accent.R, Accent.G, Accent.B, p.A * Accent.A));
		}

		return ImageTexture.CreateFromImage(image);
	}
}
