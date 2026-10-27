using Godot;
using GrimSpace.Application;
using GrimSpace.Battle.Abilities;
using GrimSpace.Battle.Player;
using GrimSpace.Components;
using GrimSpace.Units.Enums;
using GrimSpace.Units.Heat;
using GrimSpace.Units.Loadouts.Defenses;
using GrimSpace.World.StarSystem.Presentation.Ui;

namespace GrimSpace.Battle.Presentation.Ui;

public sealed partial class BattleCommandHud : Control
{
	private const int SlotSize = 64;
	private const float IconPx = 40f;
	private const float ConsoleHeight = 160f;
	private const float ResourceHeight = 128f;
	private const float ResourceWidth = 240f;
	private static readonly Color HeatBarColor = new(0.78f, 0.3f, 0.08f);
	private static readonly Color ManeuverPointsBarColor = new(0.4f, 0.3f, 0.62f);
	private const float ResourceGap = 6f;
	private const float ResourceRightPadding = 12f;
	private const float ResourceBottomPadding = 12f;
	private const float PortraitSize = 104f;
	private const string ActionPointsIconPath = "res://assets/ui/battle/action_points.svg";
	private const string ExecuteFramePath = "res://assets/ui/battle/execute_action_frame.svg";
	private const string HeatIconPath = "res://assets/ui/battle/heat.png";
	private const string ManeuverPointsIconPath = "res://assets/ui/battle/maneuver_points.png";
	private static readonly Color ActionPointsBarColor = new(0.78f, 0.12f, 0.27f);
	private static readonly Color CommandAccent =
		HudPalettes.WithAlpha(HudPalettes.PurpleRed.Highlight, 0.78f);

	private readonly ButtonGroup _modeGroup = new();
	private readonly List<AbilityCard> _abilityCards = [];
	private readonly Button _focusButton;
	private readonly Label _focusHotkey;
	private readonly Button _cycleTargetButton;
	private readonly Label _cycleTargetHotkey;
	private readonly Button _undoButton;
	private readonly Label _undoHotkey;
	private readonly Button _executeButton;
	private readonly Label _executeHotkey;
	private readonly BattleResourceBar _apBar;
	private readonly BattleResourceBar _mpBar;
	private readonly BattleResourceBar _heatBar;
	private readonly BattlePortraitHealthBar _healthBar;
	private readonly BattlePortraitShieldBar _shieldBar;
	private readonly TextureRect _pilotPortraitImage;
	private readonly Label _pilotPortraitPlaceholder;
	private string? _pilotPortraitId;
	private readonly BattleCommandConsole _console;
	private readonly HBoxContainer _consoleLayout;
	private readonly VBoxContainer _commandWing;
	private readonly MarginContainer _utilityHost;
	private readonly MarginContainer _inspectHost;
	private readonly Button _inspectBackButton;
	private readonly Label _inspectBackHotkey;
	private string? _abilityLayoutKey;

	public event Action<AbilityHudCatalog.Spec>? AbilityModeRequested;
	public event Action? FocusRequested;
	public event Action? CycleTargetRequested;
	public event Action? ReturnToPlayerRequested;
	public event Action? UndoRequested;
	public event Action? ExecuteRequested;

	public bool CanUndo => !_undoButton.Disabled;
	public bool CanExecute => !_executeButton.Disabled;

	public HBoxContainer MainActions { get; }
	public HBoxContainer UtilityActions { get; }
	public CenterContainer ExecuteAction { get; }
	public VBoxContainer ResourceMeters { get; }

	public BattleCommandHud()
	{
		MouseFilter = MouseFilterEnum.Ignore;

		_console = new BattleCommandConsole
		{
			AnchorLeft = 0f,
			AnchorTop = 1f,
			AnchorRight = 1f,
			AnchorBottom = 1f,
			OffsetLeft = 0f,
			OffsetTop = -ConsoleHeight,
			OffsetRight = 0f,
			OffsetBottom = 0f,
			GrowHorizontal = GrowDirection.Both,
			GrowVertical = GrowDirection.Begin,
		};
		AddChild(_console);
		_shieldBar = new BattlePortraitShieldBar
		{
			Name = "PortraitShieldBar",
			AnchorLeft = 0f,
			AnchorTop = 1f,
			AnchorRight = 0f,
			AnchorBottom = 1f,
			OffsetLeft = -117f,
			OffsetTop = -143f,
			OffsetRight = 49f,
			OffsetBottom = 23f,
			GrowHorizontal = GrowDirection.Begin,
			GrowVertical = GrowDirection.Begin,
		};
		AddChild(_shieldBar);
		_healthBar = new BattlePortraitHealthBar
		{
			Name = "PortraitHealthBar",
			AnchorLeft = 0f,
			AnchorTop = 1f,
			AnchorRight = 0f,
			AnchorBottom = 1f,
			OffsetLeft = -96f,
			OffsetTop = -122f,
			OffsetRight = 28f,
			OffsetBottom = 2f,
			GrowHorizontal = GrowDirection.Begin,
			GrowVertical = GrowDirection.Begin,
		};
		AddChild(_healthBar);
		var pilotPortrait = CreatePilotPortrait();
		_pilotPortraitImage = pilotPortrait.Image;
		_pilotPortraitPlaceholder = pilotPortrait.Placeholder;
		AddChild(pilotPortrait.Root);

		_consoleLayout = new HBoxContainer
		{
			MouseFilter = MouseFilterEnum.Ignore,
		};
		_consoleLayout.AddThemeConstantOverride("separation", 18);
		_console.AddChild(_consoleLayout);

		MainActions = new HBoxContainer
		{
			Name = "MainActions",
			Alignment = BoxContainer.AlignmentMode.Begin,
			MouseFilter = MouseFilterEnum.Ignore,
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
			SizeFlagsVertical = SizeFlags.ShrinkCenter,
		};
		MainActions.AddThemeConstantOverride("separation", 8);
		_consoleLayout.AddChild(MainActions);

		_commandWing = new VBoxContainer
		{
			Name = "CommandWing",
			Alignment = BoxContainer.AlignmentMode.Center,
			MouseFilter = MouseFilterEnum.Ignore,
			SizeFlagsHorizontal = SizeFlags.ShrinkEnd,
			SizeFlagsVertical = SizeFlags.ExpandFill,
		};
		_commandWing.AddThemeConstantOverride("separation", 8);
		_consoleLayout.AddChild(_commandWing);

		UtilityActions = new HBoxContainer
		{
			Name = "UtilityActions",
			Alignment = BoxContainer.AlignmentMode.Center,
			MouseFilter = MouseFilterEnum.Ignore,
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
		};
		UtilityActions.AddThemeConstantOverride("separation", 8);
		_utilityHost = new MarginContainer
		{
			Name = "UtilityHost",
			MouseFilter = MouseFilterEnum.Ignore,
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
		};
		_utilityHost.AddThemeConstantOverride("margin_left", 10);
		_utilityHost.AddChild(UtilityActions);
		_commandWing.AddChild(_utilityHost);

		ExecuteAction = new CenterContainer
		{
			Name = "ExecuteAction",
			MouseFilter = MouseFilterEnum.Ignore,
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
			SizeFlagsVertical = SizeFlags.ExpandFill,
		};
		_commandWing.AddChild(ExecuteAction);

		_inspectBackButton = new BattleHudTooltipButton
		{
			Text = BattleHudCopy.BackToPlayer.ToUpperInvariant(),
			CustomMinimumSize = new Vector2(136f, 34f),
			TooltipText = BattleHudCopy.BackToPlayerTooltip,
			FocusMode = FocusModeEnum.None,
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
			Alignment = HorizontalAlignment.Center,
			ClipContents = false,
		};
		ApplyExecuteStyles(_inspectBackButton);
		_inspectBackButton.Pressed += () => ReturnToPlayerRequested?.Invoke();
		_inspectBackHotkey = TacticalActionButtonStyle.AddHotkey(_inspectBackButton, EscapeHotkeyLabel());
		_inspectBackHotkey.OffsetLeft = -28f;
		_inspectBackHotkey.OffsetTop = -12f;
		_inspectBackHotkey.OffsetRight = -2f;
		_inspectBackHotkey.OffsetBottom = -1f;
		_inspectBackHotkey.VerticalAlignment = VerticalAlignment.Bottom;
		_inspectHost = new MarginContainer
		{
			Name = "InspectHost",
			MouseFilter = MouseFilterEnum.Ignore,
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
			Visible = false,
		};
		_inspectHost.AddThemeConstantOverride("margin_left", 10);
		_inspectHost.AddChild(_inspectBackButton);
		_commandWing.AddChild(_inspectHost);

		var resourceHost = new MarginContainer
		{
			Name = "ResourceHost",
			AnchorLeft = 1f,
			AnchorTop = 1f,
			AnchorRight = 1f,
			AnchorBottom = 1f,
			OffsetLeft = ResourceRightPadding,
			OffsetTop = -ResourceHeight - ResourceBottomPadding,
			OffsetRight = ResourceRightPadding + ResourceWidth,
			OffsetBottom = -ResourceBottomPadding,
			GrowHorizontal = GrowDirection.End,
			GrowVertical = GrowDirection.Begin,
			MouseFilter = MouseFilterEnum.Ignore,
		};
		AddChild(resourceHost);

		ResourceMeters = new VBoxContainer
		{
			Name = "ResourceMeters",
			Alignment = BoxContainer.AlignmentMode.Begin,
			MouseFilter = MouseFilterEnum.Ignore,
		};
		ResourceMeters.AddThemeConstantOverride("separation", 1);
		resourceHost.AddChild(ResourceMeters);
		_apBar = new BattleResourceBar(
			name: null,
			ActionPointsBarColor,
			BattleHudCopy.ActionPointsTooltip,
			ActionPointsIconPath,
			ActionPointsBarColor);
		ResourceMeters.AddChild(_apBar);
		_mpBar = new BattleResourceBar(
			name: null,
			color: ManeuverPointsBarColor,
			tooltip: BattleHudCopy.ManeuverPointsTooltip,
			iconPath: ManeuverPointsIconPath,
			iconTint: ManeuverPointsBarColor);
		ResourceMeters.AddChild(_mpBar);
		_heatBar = new BattleResourceBar(
			name: null,
			color: HeatBarColor,
			tooltip: BattleHudCopy.HeatTooltip(HeatSpec.Standard),
			iconPath: HeatIconPath,
			iconTint: HeatBarColor);
		ResourceMeters.AddChild(_heatBar);

		_focusButton = CreateActionButton(
			"res://assets/ui/abilities/focus.svg",
			CommandAccent,
			BattleHudCopy.FocusTooltip,
			toggleMode: false);
		_focusButton.Pressed += OnFocusPressed;
		_focusHotkey = TacticalActionButtonStyle.AddHotkey(
			_focusButton,
			GameInputBindings.PrimaryLabel("battle_focus"));
		AddUtilityAction(_focusButton);

		_cycleTargetButton = CreateActionButton(
			"res://assets/ui/battle/enemy_target_reticle.svg",
			CommandAccent,
			BattleHudCopy.CycleTargetTooltip,
			toggleMode: false);
		_cycleTargetButton.Pressed += () => CycleTargetRequested?.Invoke();
		_cycleTargetHotkey = TacticalActionButtonStyle.AddHotkey(
			_cycleTargetButton,
			GameInputBindings.PrimaryLabel("battle_cycle_enemy"));
		AddUtilityAction(_cycleTargetButton);

		_undoButton = CreateActionButton(
			"res://assets/ui/abilities/undo.svg",
			CommandAccent,
			BattleHudCopy.UndoTooltip,
			toggleMode: false);
		_undoButton.Pressed += () => UndoRequested?.Invoke();
		_undoHotkey = TacticalActionButtonStyle.AddHotkey(
			_undoButton,
			GameInputBindings.PrimaryLabel("battle_undo"));
		AddUtilityAction(_undoButton);

		_executeButton = new BattleHudTooltipButton
		{
			Text = "EXECUTE TURN",
			CustomMinimumSize = new Vector2(136f, 34f),
			TooltipText = BattleHudCopy.EndTurnTooltip,
			FocusMode = FocusModeEnum.None,
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
			Alignment = HorizontalAlignment.Center,
			ClipContents = false,
		};
		ApplyExecuteStyles(_executeButton);
		_executeButton.Pressed += () => ExecuteRequested?.Invoke();
		_executeHotkey = TacticalActionButtonStyle.AddHotkey(
			_executeButton,
			GameInputBindings.PrimaryLabel("battle_end_turn"));
		_executeHotkey.OffsetLeft = -28f;
		_executeHotkey.OffsetTop = -12f;
		_executeHotkey.OffsetRight = -2f;
		_executeHotkey.OffsetBottom = -1f;
		_executeHotkey.VerticalAlignment = VerticalAlignment.Bottom;
		ExecuteAction.AddChild(_executeButton);
		RefreshCommandWingWidth();
	}

	public void ApplyResources(
		int actionPoints,
		int maxActionPoints,
		int maneuverPoints,
		int maxManeuverPoints,
		UnitDisplayState focusState)
	{
		_apBar.Apply(actionPoints, maxActionPoints);
		_apBar.SetTooltip(BattleHudCopy.ActionPointsTooltip);
		_mpBar.Apply(maneuverPoints, maxManeuverPoints);
		_mpBar.SetTooltip(BattleHudCopy.ManeuverPointsTooltip);

		var participates = focusState.HeatProfile.Participates;
		_heatBar.Visible = participates;
		if (!participates)
			return;

		_heatBar.Apply(focusState.Heat, focusState.HeatCapacity);
		_heatBar.SetTooltip(BattleHudCopy.HeatTooltip(focusState.HeatProfile));
	}

	public void ApplyDefenses(
		int hullPoints,
		int maxHullPoints,
		FaceShieldPoints shieldPoints,
		FaceShieldPoints maxShieldPoints)
	{
		_healthBar.Set(hullPoints, maxHullPoints);
		_shieldBar.Set(shieldPoints, maxShieldPoints);
	}

	public void ApplyPilotPortrait(string? portraitId)
	{
		if (string.Equals(_pilotPortraitId, portraitId, StringComparison.Ordinal))
			return;

		_pilotPortraitId = portraitId;
		if (string.IsNullOrEmpty(portraitId))
		{
			_pilotPortraitImage.Texture = null;
			_pilotPortraitImage.Visible = false;
			_pilotPortraitPlaceholder.Visible = true;
			return;
		}

		_pilotPortraitImage.Texture = ShipPilotPortraitCatalog.TextureFor(portraitId);
		_pilotPortraitImage.Visible = true;
		_pilotPortraitPlaceholder.Visible = false;
	}

	public void ApplyMainActions(
		EPlayerMode mode,
		bool canAct,
		UnitDisplayState focusState,
		AbilityLegality abilities)
	{
		var specs = AbilityHudCatalog.ForDisplayState(focusState);
		var layoutKey = string.Join('\0', specs.Select(spec => spec.Mode.ToString()));
		if (_abilityLayoutKey != layoutKey || _abilityCards.Count != specs.Count)
			RebuildAbilityCards(specs);

		for (var i = 0; i < _abilityCards.Count; i++)
		{
			var card = _abilityCards[i];
			var state = AbilityHudCatalog.BuildState(card.Spec, focusState, abilities);
			card.Button.Disabled = !canAct || !state.Enabled;
			card.Button.SetTooltip(state.Tooltip);
			SetPressed(card.Button, mode == card.Spec.Mode);
		}

		RefreshBindingLabels();
	}

	public void ApplyCommands(
		bool isInspecting,
		bool canAct,
		bool cameraFocusAvailable,
		bool canCycleTarget,
		bool undoAvailable,
		bool allowExecute)
	{
		_utilityHost.Visible = !isInspecting;
		ExecuteAction.Visible = !isInspecting;
		_inspectHost.Visible = isInspecting;
		_focusButton.Disabled = !cameraFocusAvailable;
		_focusButton.TooltipText = BattleHudCopy.FocusTooltip;
		_cycleTargetButton.Disabled = !canCycleTarget;
		_undoButton.Disabled = !undoAvailable;
		_executeButton.Disabled = !canAct || !allowExecute;
		_inspectBackButton.Disabled = !isInspecting;
		RefreshCommandWingWidth();
	}

	public void RefreshBindingLabels()
	{
		TacticalActionButtonStyle.SetHotkey(
			_focusHotkey,
			GameInputBindings.PrimaryLabel("battle_focus"));
		TacticalActionButtonStyle.SetHotkey(
			_cycleTargetHotkey,
			GameInputBindings.PrimaryLabel("battle_cycle_enemy"));
		TacticalActionButtonStyle.SetHotkey(
			_undoHotkey,
			GameInputBindings.PrimaryLabel("battle_undo"));
		TacticalActionButtonStyle.SetHotkey(
			_executeHotkey,
			GameInputBindings.PrimaryLabel("battle_end_turn"));
		TacticalActionButtonStyle.SetHotkey(_inspectBackHotkey, EscapeHotkeyLabel());
		for (var i = 0; i < _abilityCards.Count; i++)
			TacticalActionButtonStyle.SetHotkey(
				_abilityCards[i].Hotkey,
				GameInputBindings.PrimaryLabel(GameInputBindings.AbilityAction(i + 1)));
	}

	public bool TryFocus()
	{
		if (_focusButton.Disabled)
			return false;
		OnFocusPressed();
		return true;
	}

	public bool TryUndo()
	{
		if (_undoButton.Disabled)
			return false;
		UndoRequested?.Invoke();
		return true;
	}

	public bool TryExecute()
	{
		if (_executeButton.Disabled)
			return false;
		ExecuteRequested?.Invoke();
		return true;
	}

	public bool TryActivateAbilityHotkey(int slot)
	{
		var index = slot - 1;
		if (index < 0 || index >= _abilityCards.Count)
			return false;

		var button = _abilityCards[index].Button;
		if (button.Disabled)
			return false;
		button.ButtonPressed = true;
		return true;
	}

	private void RebuildAbilityCards(IReadOnlyList<AbilityHudCatalog.Spec> specs)
	{
		foreach (var card in _abilityCards)
			card.Button.QueueFree();
		_abilityCards.Clear();
		_abilityLayoutKey = string.Join('\0', specs.Select(spec => spec.Mode.ToString()));
		UpdateConsoleWidth(specs.Count);

		for (var i = 0; i < specs.Count; i++)
		{
			var spec = specs[i];
			var button = CreateActionButton(
				spec.IconPath,
				spec.IconTint,
				"Ability",
				toggleMode: true);
			button.ButtonGroup = _modeGroup;
			var hotkey = TacticalActionButtonStyle.AddHotkey(
				button,
				GameInputBindings.PrimaryLabel(GameInputBindings.AbilityAction(i + 1)));
			button.Toggled += pressed =>
			{
				if (pressed)
					AbilityModeRequested?.Invoke(spec);
			};
			MainActions.AddChild(button);
			_abilityCards.Add(new AbilityCard(button, hotkey, spec));
		}
	}

	private void OnFocusPressed() => FocusRequested?.Invoke();

	private static string EscapeHotkeyLabel()
	{
		var label = OS.GetKeycodeString(Key.Escape);
		return string.IsNullOrEmpty(label) ? "Esc" : label;
	}

	private static BattleHudTooltipButton CreateActionButton(
		string? iconPath,
		Color accent,
		string tooltip,
		bool toggleMode)
	{
		var button = new BattleHudTooltipButton
		{
			CustomMinimumSize = new Vector2(SlotSize, SlotSize),
			Icon = SvgIconLoader.LoadCentered(iconPath, accent, (int)IconPx),
			ExpandIcon = false,
			IconAlignment = HorizontalAlignment.Center,
			VerticalIconAlignment = VerticalAlignment.Center,
			Alignment = HorizontalAlignment.Center,
			TooltipText = tooltip,
			FocusMode = FocusModeEnum.None,
			ToggleMode = toggleMode,
			ClipContents = false,
		};
		button.AddThemeConstantOverride("h_separation", 0);
		TacticalActionButtonStyle.Apply(button);
		return button;
	}

	private static (PanelContainer Root, TextureRect Image, Label Placeholder) CreatePilotPortrait()
	{
		var portrait = new PanelContainer
		{
			Name = "PilotPortrait",
			AnchorLeft = 0f,
			AnchorTop = 1f,
			AnchorRight = 0f,
			AnchorBottom = 1f,
			OffsetLeft = -PortraitSize + 18f,
			OffsetTop = -PortraitSize - 8f,
			OffsetRight = 18f,
			OffsetBottom = -8f,
			GrowHorizontal = GrowDirection.Begin,
			GrowVertical = GrowDirection.Begin,
			MouseFilter = MouseFilterEnum.Ignore,
		};
		portrait.AddThemeStyleboxOverride(
			"panel",
			CircleStyle(
				HudPalettes.WithAlpha(HudPalettes.PurpleRed.Surface, 0.98f),
				HudPalettes.WithAlpha(HudPalettes.PurpleRed.Primary, 0.95f),
				3,
				7f));

		var inner = new PanelContainer
		{
			MouseFilter = MouseFilterEnum.Ignore,
			ClipContents = true,
		};
		inner.AddThemeStyleboxOverride(
			"panel",
			CircleStyle(
				HudPalettes.PurpleRed.DeepSurface,
				HudPalettes.WithAlpha(HudPalettes.PurpleRed.Border, 0.9f),
				2,
				6f));
		portrait.AddChild(inner);

		var image = new TextureRect
		{
			AnchorRight = 1f,
			AnchorBottom = 1f,
			OffsetLeft = 0f,
			OffsetTop = 0f,
			OffsetRight = 0f,
			OffsetBottom = 0f,
			GrowHorizontal = GrowDirection.Both,
			GrowVertical = GrowDirection.Both,
			MouseFilter = MouseFilterEnum.Ignore,
			ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
			StretchMode = TextureRect.StretchModeEnum.KeepAspectCovered,
			Visible = false,
		};
		inner.AddChild(image);

		var placeholder = new Label
		{
			Text = "PILOT",
			AnchorRight = 1f,
			AnchorBottom = 1f,
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Center,
			MouseFilter = MouseFilterEnum.Ignore,
		};
		placeholder.AddThemeColorOverride(
			"font_color",
			HudPalettes.WithAlpha(HudPalettes.PurpleRed.Primary, 0.72f));
		placeholder.AddThemeColorOverride(
			"font_outline_color",
			HudPalettes.PurpleRed.DeepSurface);
		placeholder.AddThemeConstantOverride("outline_size", 2);
		placeholder.AddThemeFontSizeOverride("font_size", 12);
		inner.AddChild(placeholder);
		return (portrait, image, placeholder);
	}

	private static StyleBoxFlat CircleStyle(
		Color fill,
		Color border,
		int borderWidth,
		float contentMargin = 0f) =>
		new()
		{
			BgColor = fill,
			BorderColor = border,
			BorderWidthLeft = borderWidth,
			BorderWidthTop = borderWidth,
			BorderWidthRight = borderWidth,
			BorderWidthBottom = borderWidth,
			CornerRadiusTopLeft = 999,
			CornerRadiusTopRight = 999,
			CornerRadiusBottomRight = 999,
			CornerRadiusBottomLeft = 999,
			AntiAliasing = true,
			ContentMarginLeft = contentMargin,
			ContentMarginTop = contentMargin,
			ContentMarginRight = contentMargin,
			ContentMarginBottom = contentMargin,
		};

	private static void SetPressed(Button button, bool pressed)
	{
		button.SetBlockSignals(true);
		button.ButtonPressed = pressed;
		button.SetBlockSignals(false);
	}

	private void AddUtilityAction(Button button)
	{
		UtilityActions.AddChild(button);
		RefreshCommandWingWidth();
	}

	private void RefreshCommandWingWidth()
	{
		var utilityWidth =
			_utilityHost.GetThemeConstant("margin_left")
			+ ChildControlsWidth(
				UtilityActions,
				UtilityActions.GetThemeConstant("separation"))
			+ _utilityHost.GetThemeConstant("margin_right");
		var inspectWidth =
			_inspectHost.GetThemeConstant("margin_left")
			+ ChildControlsWidth(_inspectHost, separation: 0)
			+ _inspectHost.GetThemeConstant("margin_right");
		var width = Mathf.Max(
			Mathf.Max(utilityWidth, inspectWidth),
			ChildControlsWidth(ExecuteAction, separation: 0));
		if (width <= 0f)
			return;

		_commandWing.CustomMinimumSize = new Vector2(width, 0f);
		_console.SetWingContentWidth(width);
		UpdateConsoleWidth(_abilityCards.Count);
	}

	private static float ChildControlsWidth(Node parent, int separation)
	{
		var width = 0f;
		var count = 0;
		foreach (var child in parent.GetChildren())
		{
			if (child is not Control { Visible: true } control)
				continue;

			width += control.GetCombinedMinimumSize().X;
			count++;
		}

		return width + System.Math.Max(0, count - 1) * separation;
	}

	private void UpdateConsoleWidth(int abilityCount)
	{
		var cardCount = abilityCount;
		var mainWidth = cardCount * SlotSize + System.Math.Max(0, cardCount - 1) * 8f;
		var width =
			_console.HorizontalContentMargins
			+ mainWidth
			+ _consoleLayout.GetThemeConstant("separation")
			+ _commandWing.CustomMinimumSize.X;
		CustomMinimumSize = new Vector2(width, ConsoleHeight + ResourceGap + ResourceHeight);
		OffsetLeft = -width * 0.5f;
		OffsetRight = width * 0.5f;
		OffsetTop = -ConsoleHeight - ResourceGap - ResourceHeight;
		OffsetBottom = 0f;
	}

	private static void ApplyExecuteStyles(Button button)
	{
		var texture = GD.Load<Texture2D>(ExecuteFramePath)
			?? throw new InvalidOperationException($"Could not load execute frame '{ExecuteFramePath}'.");
		TacticalActionButtonStyle.ApplyInteractionStates(
			button,
			color => ExecuteStyle(texture, color));
		button.AddThemeColorOverride(
			"font_outline_color",
			HudPalettes.WithAlpha(HudPalettes.PurpleRed.DeepSurface, 0.95f));
		button.AddThemeConstantOverride("outline_size", 1);
		button.AddThemeFontSizeOverride("font_size", 13);
	}

	private static StyleBoxTexture ExecuteStyle(Texture2D texture, Color modulate) =>
		new()
		{
			Texture = texture,
			ModulateColor = modulate,
			TextureMarginLeft = 10,
			TextureMarginTop = 8,
			TextureMarginRight = 10,
			TextureMarginBottom = 8,
			ContentMarginLeft = 12,
			ContentMarginRight = 12,
			ContentMarginTop = 4,
			ContentMarginBottom = 4,
		};

	private sealed record AbilityCard(
		BattleHudTooltipButton Button,
		Label Hotkey,
		AbilityHudCatalog.Spec Spec);
}
