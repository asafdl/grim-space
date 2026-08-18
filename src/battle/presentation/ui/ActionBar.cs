using Godot;
using GrimSpace.Units.Enums;

namespace GrimSpace.Battle.Presentation.Ui;

public sealed partial class ActionBar : HBoxContainer
{
	public event Action<AbilityHudCatalog.Spec>? AbilityModeRequested;
	public event Action? EndTurnRequested;

	public ActionInstructionBar InstructionBar { get; private set; } = null!;

	private const int SlotSize = 64;
	private const float IconPx = 40f;

	private readonly ButtonGroup _modeGroup;
	private readonly List<AbilitySlot> _abilitySlots = [];
	private HBoxContainer _abilityRow = null!;
	private PanelContainer _abilityPanel = null!;
	private VBoxContainer _actionStack = null!;
	private PanelContainer _endTurnPanel = null!;
	private Button _endTurnButton = null!;
	private EType? _layoutType;

	public ActionBar(ButtonGroup modeGroup)
	{
		_modeGroup = modeGroup;
		MouseFilter = MouseFilterEnum.Ignore;
		Alignment = AlignmentMode.Begin;
		AddThemeConstantOverride("separation", 14);
		Build();
	}

	public void ApplyLayout(EType unitType, IReadOnlyList<AbilityHudCatalog.Spec> specs)
	{
		if (_layoutType == unitType && _abilitySlots.Count == specs.Count)
			return;

		_layoutType = unitType;
		RebuildAbilityRow(specs);
	}

	public void SetMode(EPlayerMode mode)
	{
		foreach (var slot in _abilitySlots)
		{
			slot.Button.SetBlockSignals(true);
			slot.Button.ButtonPressed = mode == slot.Spec.Mode;
			slot.Button.SetBlockSignals(false);
		}
	}

	public void Configure(
		bool canAct,
		bool isInspecting,
		IReadOnlyList<AbilityBarSlotState> slots)
	{
		_endTurnPanel.Visible = !isInspecting;
		_endTurnButton.Disabled = !canAct;

		for (var i = 0; i < _abilitySlots.Count && i < slots.Count; i++)
		{
			var slot = _abilitySlots[i];
			var state = slots[i];
			slot.Button.Disabled = !canAct || !state.Enabled;
			slot.Charges.Text = state.Charges;
			slot.Button.TooltipText = state.Tooltip;
		}
	}

	/// <summary>Activates ability hotkey slot 2–4 if enabled. Returns false if unbound or disabled.</summary>
	public bool TryActivateHotkey(int slot)
	{
		var index = slot - 2;
		if (index < 0 || index >= _abilitySlots.Count)
			return false;

		var button = _abilitySlots[index].Button;
		if (button.Disabled)
			return false;

		button.ButtonPressed = true;
		return true;
	}

	private void RebuildAbilityRow(IReadOnlyList<AbilityHudCatalog.Spec> specs)
	{
		foreach (var slot in _abilitySlots)
			slot.Button.QueueFree();
		_abilitySlots.Clear();

		var abilityAccent = new Color(0.55f, 0.78f, 1f);
		for (var i = 0; i < specs.Count; i++)
		{
			var spec = specs[i];
			var hotkey = (i + 2).ToString();
			var button = CreateAbilitySlot(
				hotkey,
				spec.Tooltip,
				spec.IconPath,
				abilityAccent,
				spec,
				out var charges);
			_abilityRow.AddChild(button);
			_abilitySlots.Add(new AbilitySlot(button, charges, spec));
		}
	}

	private void Build()
	{
		_actionStack = new VBoxContainer
		{
			Alignment = AlignmentMode.Center,
			SizeFlagsHorizontal = SizeFlags.ShrinkCenter,
		};
		_actionStack.AddThemeConstantOverride("separation", 6);
		AddChild(_actionStack);

		_abilityPanel = CreatePanel(
			new Color(0.08f, 0.1f, 0.14f, 0.92f),
			new Color(0.35f, 0.55f, 0.85f, 0.75f));
		_actionStack.AddChild(_abilityPanel);

		var abilityPad = CreatePad();
		_abilityPanel.AddChild(abilityPad);

		_abilityRow = new HBoxContainer
		{
			Alignment = AlignmentMode.Center,
		};
		_abilityRow.AddThemeConstantOverride("separation", 8);
		abilityPad.AddChild(_abilityRow);

		InstructionBar = new ActionInstructionBar
		{
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
		};
		_actionStack.AddChild(InstructionBar);

		_endTurnPanel = CreatePanel(
			new Color(0.16f, 0.1f, 0.05f, 0.94f),
			new Color(0.95f, 0.7f, 0.25f, 0.9f));
		_endTurnPanel.SizeFlagsVertical = SizeFlags.ShrinkBegin;
		AddChild(_endTurnPanel);

		var endPad = CreatePad();
		_endTurnPanel.AddChild(endPad);

		_endTurnButton = CreateEndTurnButton();
		endPad.AddChild(_endTurnButton);
	}

	private Button CreateAbilitySlot(
		string hotkey,
		string tooltip,
		string? iconPath,
		Color accent,
		AbilityHudCatalog.Spec spec,
		out Label charges)
	{
		var button = new Button
		{
			ToggleMode = true,
			ButtonGroup = _modeGroup,
			CustomMinimumSize = new Vector2(SlotSize, SlotSize),
			Icon = SvgIconLoader.Load(iconPath, accent, (int)IconPx),
			ExpandIcon = false,
			IconAlignment = HorizontalAlignment.Center,
			VerticalIconAlignment = VerticalAlignment.Center,
			Alignment = HorizontalAlignment.Center,
			TooltipText = tooltip,
			FocusMode = FocusModeEnum.None,
			ClipContents = false,
		};
		button.AddThemeConstantOverride("h_separation", 0);

		ApplySlotStyles(button, accent);
		AddHotkeyBadge(button, hotkey);
		charges = AddChargeBadge(button);

		button.Toggled += pressed =>
		{
			if (pressed)
				AbilityModeRequested?.Invoke(spec);
		};
		return button;
	}

	private Button CreateEndTurnButton()
	{
		var button = new Button
		{
			Text = BattleHudCopy.EndTurn,
			CustomMinimumSize = new Vector2(112, SlotSize),
			TooltipText = BattleHudCopy.EndTurnTooltip,
			FocusMode = FocusModeEnum.None,
			ClipContents = false,
		};

		var normal = MakeStyle(new Color(0.28f, 0.16f, 0.05f, 1f), new Color(0.95f, 0.7f, 0.25f), 2, 6);
		var hover = MakeStyle(new Color(0.38f, 0.22f, 0.08f, 1f), new Color(1f, 0.82f, 0.35f), 2, 6);
		var pressed = MakeStyle(new Color(0.45f, 0.28f, 0.1f, 1f), new Color(1f, 0.9f, 0.45f), 2, 6);
		var disabled = MakeStyle(new Color(0.12f, 0.1f, 0.08f, 0.85f), new Color(0.35f, 0.3f, 0.22f), 2, 6);

		button.AddThemeStyleboxOverride("normal", normal);
		button.AddThemeStyleboxOverride("hover", hover);
		button.AddThemeStyleboxOverride("pressed", pressed);
		button.AddThemeStyleboxOverride("disabled", disabled);
		button.AddThemeStyleboxOverride("focus", (StyleBox)normal.Duplicate());
		button.AddThemeColorOverride("font_color", new Color(1f, 0.9f, 0.65f));
		button.AddThemeColorOverride("font_hover_color", new Color(1f, 0.95f, 0.75f));
		button.AddThemeColorOverride("font_pressed_color", new Color(1f, 1f, 0.85f));
		button.AddThemeColorOverride("font_disabled_color", new Color(0.55f, 0.5f, 0.4f));
		button.AddThemeFontSizeOverride("font_size", 14);

		AddHotkeyBadge(button, "Space");

		button.Pressed += () => EndTurnRequested?.Invoke();
		return button;
	}

	private static void AddHotkeyBadge(Button button, string text)
	{
		var width = text.Length <= 1 ? 16f : 8f + text.Length * 6.5f;
		var height = 14f;
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
			ContentMarginTop = 0,
			ContentMarginBottom = 0,
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

	private static Label AddChargeBadge(Button button)
	{
		const float width = 28f;
		const float height = 14f;
		var badge = new PanelContainer
		{
			MouseFilter = MouseFilterEnum.Ignore,
			CustomMinimumSize = new Vector2(width, height),
			AnchorLeft = 0.5f,
			AnchorTop = 1f,
			AnchorRight = 0.5f,
			AnchorBottom = 1f,
			OffsetLeft = -width * 0.5f,
			OffsetTop = -height * 0.5f,
			OffsetRight = width * 0.5f,
			OffsetBottom = height * 0.5f,
			GrowHorizontal = GrowDirection.Both,
		};
		badge.AddThemeStyleboxOverride("panel", new StyleBoxFlat
		{
			BgColor = new Color(0.06f, 0.1f, 0.08f, 0.95f),
			BorderColor = new Color(0.55f, 0.95f, 0.65f, 0.85f),
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
			ContentMarginTop = 0,
			ContentMarginBottom = 0,
		});

		var label = new Label
		{
			Text = "0/0",
			MouseFilter = MouseFilterEnum.Ignore,
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Center,
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
			SizeFlagsVertical = SizeFlags.ExpandFill,
		};
		label.AddThemeFontSizeOverride("font_size", 10);
		label.AddThemeColorOverride("font_color", new Color(0.7f, 1f, 0.8f));
		badge.AddChild(label);
		button.AddChild(badge);
		return label;
	}

	private static void ApplySlotStyles(Button button, Color accent)
	{
		var normal = MakeStyle(new Color(0.12f, 0.14f, 0.2f, 1f), accent * new Color(1f, 1f, 1f, 0.55f), 2, 4);
		var hover = MakeStyle(new Color(0.18f, 0.22f, 0.3f, 1f), accent, 2, 4);
		var pressed = MakeStyle(new Color(0.22f, 0.28f, 0.4f, 1f), accent, 3, 4);
		var disabled = MakeStyle(new Color(0.08f, 0.09f, 0.11f, 0.85f), new Color(0.25f, 0.28f, 0.32f), 2, 4);

		button.AddThemeStyleboxOverride("normal", normal);
		button.AddThemeStyleboxOverride("hover", hover);
		button.AddThemeStyleboxOverride("pressed", pressed);
		button.AddThemeStyleboxOverride("disabled", disabled);
		button.AddThemeStyleboxOverride("focus", (StyleBox)pressed.Duplicate());
	}

	private static PanelContainer CreatePanel(Color bg, Color border)
	{
		var panel = new PanelContainer();
		panel.AddThemeStyleboxOverride("panel", MakeStyle(bg, border, 2, 8));
		return panel;
	}

	private static MarginContainer CreatePad()
	{
		var pad = new MarginContainer();
		pad.AddThemeConstantOverride("margin_left", 8);
		pad.AddThemeConstantOverride("margin_right", 8);
		pad.AddThemeConstantOverride("margin_top", 8);
		pad.AddThemeConstantOverride("margin_bottom", 8);
		return pad;
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

	private sealed record AbilitySlot(Button Button, Label Charges, AbilityHudCatalog.Spec Spec);
}
