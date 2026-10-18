using Godot;
using GrimSpace.Application;
using GrimSpace.Battle.Abilities;
using GrimSpace.Battle.Player;
using GrimSpace.Components;
using GrimSpace.Units.Enums;

namespace GrimSpace.Battle.Presentation.Ui;

public sealed partial class BattleCommandHud : Control
{
	private const int SlotSize = 64;
	private const float IconPx = 40f;
	private const float ConsoleHeight = 120f;
	private const float ResourceHeight = 48f;
	private const float ResourceWidth = 154f;
	private const float ResourceGap = 6f;
	private const float CommandWingWidth = 172f;
	private const float ConsoleHorizontalMargins = 36f;
	private const float ConsoleRegionSeparation = 18f;
	private const string ExecuteFramePath = "res://assets/ui/battle/execute_action_frame.svg";
	private static readonly Color MoveAccent = new(0.55f, 0.78f, 1f);
	private static readonly Color CommandAccent = new(0.88f, 0.32f, 0.42f, 0.78f);

	private readonly ButtonGroup _modeGroup = new();
	private readonly List<AbilityCard> _abilityCards = [];
	private readonly Button _moveButton;
	private readonly Label _moveHotkey;
	private readonly Button _focusButton;
	private readonly Label _focusHotkey;
	private readonly Button _undoButton;
	private readonly Label _undoHotkey;
	private readonly Button _executeButton;
	private readonly Label _executeHotkey;
	private readonly BattleResourceBar _apBar;
	private readonly BattleResourceBar _mpBar;
	private string? _abilityLayoutKey;
	private bool _isInspecting;

	public event Action? MoveModeRequested;
	public event Action<AbilityHudCatalog.Spec>? AbilityModeRequested;
	public event Action? FocusRequested;
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

		var console = new BattleCommandConsole
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
		AddChild(console);

		var consoleLayout = new HBoxContainer
		{
			MouseFilter = MouseFilterEnum.Ignore,
		};
		consoleLayout.AddThemeConstantOverride("separation", 18);
		console.AddChild(consoleLayout);

		MainActions = new HBoxContainer
		{
			Name = "MainActions",
			Alignment = BoxContainer.AlignmentMode.Begin,
			MouseFilter = MouseFilterEnum.Ignore,
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
			SizeFlagsVertical = SizeFlags.ShrinkCenter,
		};
		MainActions.AddThemeConstantOverride("separation", 8);
		consoleLayout.AddChild(MainActions);

		var commandWing = new VBoxContainer
		{
			Name = "CommandWing",
			CustomMinimumSize = new Vector2(CommandWingWidth, 0f),
			Alignment = BoxContainer.AlignmentMode.Center,
			MouseFilter = MouseFilterEnum.Ignore,
			SizeFlagsHorizontal = SizeFlags.ShrinkEnd,
			SizeFlagsVertical = SizeFlags.ExpandFill,
		};
		commandWing.AddThemeConstantOverride("separation", 8);
		consoleLayout.AddChild(commandWing);

		UtilityActions = new HBoxContainer
		{
			Name = "UtilityActions",
			Alignment = BoxContainer.AlignmentMode.Center,
			MouseFilter = MouseFilterEnum.Ignore,
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
		};
		UtilityActions.AddThemeConstantOverride("separation", 8);
		commandWing.AddChild(UtilityActions);

		ExecuteAction = new CenterContainer
		{
			Name = "ExecuteAction",
			MouseFilter = MouseFilterEnum.Ignore,
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
			SizeFlagsVertical = SizeFlags.ExpandFill,
		};
		commandWing.AddChild(ExecuteAction);

		var resourceHost = new MarginContainer
		{
			Name = "ResourceHost",
			AnchorLeft = 0f,
			AnchorTop = 1f,
			AnchorRight = 0f,
			AnchorBottom = 1f,
			OffsetLeft = 0f,
			OffsetTop = -ConsoleHeight - ResourceGap - ResourceHeight,
			OffsetRight = ResourceWidth,
			OffsetBottom = -ConsoleHeight - ResourceGap,
			GrowHorizontal = GrowDirection.Both,
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
		ResourceMeters.AddThemeConstantOverride("separation", 3);
		resourceHost.AddChild(ResourceMeters);
		_apBar = new BattleResourceBar(
			"AP",
			new Color(0.79f, 0.48f, 0.61f),
			BattleHudCopy.ActionPointsTooltip);
		ResourceMeters.AddChild(_apBar);
		_mpBar = new BattleResourceBar(
			"MP",
			new Color(0.52f, 0.45f, 0.7f),
			BattleHudCopy.ManeuverPointsTooltip);
		ResourceMeters.AddChild(_mpBar);

		_moveButton = CreateActionButton(
			"res://assets/ui/abilities/move.svg",
			MoveAccent,
			BattleHudCopy.MoveTooltip,
			toggleMode: true);
		_moveButton.ButtonGroup = _modeGroup;
		_moveButton.ButtonPressed = true;
		_moveButton.Toggled += pressed =>
		{
			if (pressed)
				MoveModeRequested?.Invoke();
		};
		_moveHotkey = TacticalActionButtonStyle.AddHotkey(
			_moveButton,
			GameInputBindings.PrimaryLabel("battle_move_mode"));
		MainActions.AddChild(_moveButton);

		_focusButton = CreateActionButton(
			"res://assets/ui/abilities/focus.svg",
			CommandAccent,
			BattleHudCopy.FocusTooltip,
			toggleMode: false);
		_focusButton.Pressed += OnFocusPressed;
		_focusHotkey = TacticalActionButtonStyle.AddHotkey(
			_focusButton,
			GameInputBindings.PrimaryLabel("battle_focus"));
		UtilityActions.AddChild(_focusButton);

		_undoButton = CreateActionButton(
			"res://assets/ui/abilities/undo.svg",
			CommandAccent,
			BattleHudCopy.UndoTooltip,
			toggleMode: false);
		_undoButton.Pressed += () => UndoRequested?.Invoke();
		_undoHotkey = TacticalActionButtonStyle.AddHotkey(
			_undoButton,
			GameInputBindings.PrimaryLabel("battle_undo"));
		UtilityActions.AddChild(_undoButton);

		_executeButton = new Button
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
		UpdateConsoleWidth(abilityCount: 0);
	}

	public void ApplyResources(
		int actionPoints,
		int maxActionPoints,
		int maneuverPoints,
		int maxManeuverPoints)
	{
		_apBar.Apply(actionPoints, maxActionPoints);
		_mpBar.Apply(maneuverPoints, maxManeuverPoints);
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

		_moveButton.Disabled = !canAct;
		SetPressed(_moveButton, mode == EPlayerMode.Move);

		for (var i = 0; i < _abilityCards.Count; i++)
		{
			var card = _abilityCards[i];
			var state = AbilityHudCatalog.BuildState(card.Spec, focusState, abilities);
			card.Button.Disabled = !canAct || !state.Enabled;
			card.Button.TooltipText = state.Tooltip;
			SetPressed(card.Button, mode == card.Spec.Mode);
		}

		RefreshBindingLabels();
	}

	public void ApplyCommands(
		bool isInspecting,
		bool canAct,
		bool cameraFocusAvailable,
		bool undoAvailable,
		bool allowExecute)
	{
		_isInspecting = isInspecting;
		_focusButton.Disabled = isInspecting || !cameraFocusAvailable;
		if (isInspecting)
			_focusButton.Disabled = false;
		_focusButton.TooltipText = isInspecting
			? BattleHudCopy.BackToPlayerTooltip
			: BattleHudCopy.FocusTooltip;
		_undoButton.Disabled = isInspecting || !undoAvailable;
		_executeButton.Disabled = isInspecting || !canAct || !allowExecute;
	}

	public void RefreshBindingLabels()
	{
		TacticalActionButtonStyle.SetHotkey(
			_moveHotkey,
			GameInputBindings.PrimaryLabel("battle_move_mode"));
		TacticalActionButtonStyle.SetHotkey(
			_focusHotkey,
			GameInputBindings.PrimaryLabel("battle_focus"));
		TacticalActionButtonStyle.SetHotkey(
			_undoHotkey,
			GameInputBindings.PrimaryLabel("battle_undo"));
		TacticalActionButtonStyle.SetHotkey(
			_executeHotkey,
			GameInputBindings.PrimaryLabel("battle_end_turn"));
		for (var i = 0; i < _abilityCards.Count; i++)
			TacticalActionButtonStyle.SetHotkey(
				_abilityCards[i].Hotkey,
				GameInputBindings.PrimaryLabel($"battle_ability_{i + 2}"));
	}

	public bool TryActivateMove()
	{
		if (_moveButton.Disabled)
			return false;
		_moveButton.ButtonPressed = true;
		return true;
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
		var index = slot - 2;
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
				GameInputBindings.PrimaryLabel($"battle_ability_{i + 2}"));
			button.Toggled += pressed =>
			{
				if (pressed)
					AbilityModeRequested?.Invoke(spec);
			};
			MainActions.AddChild(button);
			_abilityCards.Add(new AbilityCard(button, hotkey, spec));
		}
	}

	private void OnFocusPressed()
	{
		if (_isInspecting)
			ReturnToPlayerRequested?.Invoke();
		else
			FocusRequested?.Invoke();
	}

	private static Button CreateActionButton(
		string? iconPath,
		Color accent,
		string tooltip,
		bool toggleMode)
	{
		var button = new Button
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
		TacticalActionButtonStyle.Apply(button, accent);
		return button;
	}

	private static void SetPressed(Button button, bool pressed)
	{
		button.SetBlockSignals(true);
		button.ButtonPressed = pressed;
		button.SetBlockSignals(false);
	}

	private void UpdateConsoleWidth(int abilityCount)
	{
		var cardCount = abilityCount + 1;
		var mainWidth = cardCount * SlotSize + System.Math.Max(0, cardCount - 1) * 8f;
		var width =
			ConsoleHorizontalMargins
			+ mainWidth
			+ ConsoleRegionSeparation
			+ CommandWingWidth;
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
		button.AddThemeStyleboxOverride("normal", ExecuteStyle(texture, Colors.White));
		button.AddThemeStyleboxOverride(
			"hover",
			ExecuteStyle(texture, new Color(1f, 0.9f, 0.92f)));
		button.AddThemeStyleboxOverride(
			"pressed",
			ExecuteStyle(texture, new Color(1f, 0.62f, 0.68f)));
		button.AddThemeStyleboxOverride(
			"disabled",
			ExecuteStyle(texture, new Color(0.48f, 0.45f, 0.42f, 0.62f)));
		button.AddThemeStyleboxOverride("focus", new StyleBoxEmpty());
		button.AddThemeColorOverride("font_color", new Color(0.98f, 0.9f, 0.92f));
		button.AddThemeColorOverride("font_hover_color", Colors.White);
		button.AddThemeColorOverride("font_pressed_color", Colors.White);
		button.AddThemeColorOverride("font_disabled_color", new Color(0.5f, 0.47f, 0.42f));
		button.AddThemeColorOverride("font_outline_color", new Color(0.08f, 0.015f, 0.025f, 0.95f));
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
		Button Button,
		Label Hotkey,
		AbilityHudCatalog.Spec Spec);
}
