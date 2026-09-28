using System.Text;
using GrimSpace.Components;
using Godot;

namespace GrimSpace.Application;

public enum BindingContext
{
	WorldMap,
	Battle,
}

public enum BindingDevice
{
	Keyboard,
	Mouse,
}

public enum KeyKind
{
	Physical,
	Logical,
}

public enum ModifierKind
{
	None,
	Ctrl,
	Meta,
	Shift,
	Alt,
	Primary,
}

public enum BindingCapturePolicy
{
	KeyboardOnly,
	MouseOnly,
	Any,
}

public readonly record struct KeyBinding(
	KeyKind Kind = KeyKind.Physical,
	Key Key = Key.None,
	ModifierKind Modifier = ModifierKind.None,
	BindingDevice Device = BindingDevice.Keyboard,
	MouseButton MouseButton = MouseButton.None)
{
	public bool IsEmpty => Device switch
	{
		BindingDevice.Keyboard => Key == Key.None,
		BindingDevice.Mouse => MouseButton == MouseButton.None,
		_ => true,
	};

	public static KeyBinding Empty => new();

	public static KeyBinding Mouse(MouseButton button) =>
		new(KeyKind.Physical, Key.None, ModifierKind.None, BindingDevice.Mouse, button);
}

public sealed record BindingSpec(
	StringName Action,
	BindingContext Context,
	string Label,
	KeyBinding Primary,
	KeyBinding? Secondary = null,
	BindingCapturePolicy CapturePolicy = BindingCapturePolicy.KeyboardOnly,
	string? ConflictGroup = null);

public readonly record struct BindingPair(
	KeyBinding Primary,
	KeyBinding? Secondary = null);

public static class GameInputBindings
{
	private const int BindingsVersion = 2;

	public static IReadOnlyList<BindingSpec> Specs { get; } = BuildSpecs();

	private static IReadOnlyDictionary<StringName, BindingPair> _current =
		DefaultBindings();

	public static IReadOnlyDictionary<StringName, BindingPair> Current => _current;

	public static IReadOnlyDictionary<StringName, BindingPair> DefaultBindings()
	{
		var result = new Dictionary<StringName, BindingPair>(Specs.Count);
		foreach (var spec in Specs)
			result[spec.Action] = new BindingPair(spec.Primary, spec.Secondary);
		return result;
	}

	public static void Apply(IReadOnlyDictionary<StringName, BindingPair> bindings)
	{
		foreach (var spec in Specs)
		{
			if (!InputMap.HasAction(spec.Action))
				InputMap.AddAction(spec.Action);

			InputMap.ActionEraseEvents(spec.Action);

			var pair = bindings[spec.Action];
			if (!pair.Primary.IsEmpty)
				InputMap.ActionAddEvent(spec.Action, ToInputEvent(pair.Primary));

			if (pair.Secondary is { } secondary && !secondary.IsEmpty)
				InputMap.ActionAddEvent(spec.Action, ToInputEvent(secondary));
		}

		_current = Copy(bindings);
	}

	public static string Label(StringName action)
	{
		if (!_current.TryGetValue(action, out var pair))
			return "?";

		return LabelForBinding(pair.Primary);
	}

	public static string LabelForBinding(KeyBinding binding) => FormatBinding(binding);

	public static bool TryFindSpec(StringName action, out BindingSpec spec)
	{
		foreach (var candidate in Specs)
		{
			if (candidate.Action == action)
			{
				spec = candidate;
				return true;
			}
		}

		spec = null!;
		return false;
	}

	public static bool AllowsDevice(BindingCapturePolicy policy, BindingDevice device) =>
		policy switch
		{
			BindingCapturePolicy.KeyboardOnly => device == BindingDevice.Keyboard,
			BindingCapturePolicy.MouseOnly => device == BindingDevice.Mouse,
			BindingCapturePolicy.Any => true,
			_ => false,
		};

	public static bool IsReserved(BindingContext context, KeyBinding binding)
	{
		if (binding.IsEmpty || binding.Device != BindingDevice.Keyboard)
			return false;

		if (binding.Key == Key.F10)
			return true;

		if (binding.Key == Key.Escape && binding.Modifier == ModifierKind.None)
			return true;

		if (context == BindingContext.WorldMap
			&& binding.Modifier == ModifierKind.Shift
			&& binding.Key is Key.F or Key.B or Key.L)
			return true;

		return false;
	}

	public static bool TryCaptureKeyboard(
		InputEventKey keyEvent,
		out KeyBinding binding)
	{
		binding = default;

		if (!keyEvent.Pressed || keyEvent.Echo)
			return false;

		if (keyEvent.Keycode is Key.Tab or Key.Enter or Key.KpEnter)
			return false;

		var modifier = ReadModifier(keyEvent);
		var key = keyEvent.Keycode != Key.None ? keyEvent.Keycode : keyEvent.PhysicalKeycode;
		if (key == Key.None || IsModifierOnlyKey(key))
			return false;

		var kind = PreferPhysicalKind(key) ? KeyKind.Physical : KeyKind.Logical;
		if (kind == KeyKind.Physical)
			key = keyEvent.PhysicalKeycode != Key.None ? keyEvent.PhysicalKeycode : key;

		binding = new KeyBinding(kind, key, modifier);
		return true;
	}

	public static bool TryCaptureMouse(
		InputEventMouseButton mouseEvent,
		out KeyBinding binding)
	{
		binding = default;

		if (!mouseEvent.Pressed)
			return false;

		if (mouseEvent.ButtonIndex == MouseButton.None)
			return false;

		binding = KeyBinding.Mouse(mouseEvent.ButtonIndex);
		return true;
	}

	public static bool TryAssign(
		IReadOnlyDictionary<StringName, BindingPair> draft,
		StringName action,
		bool secondarySlot,
		KeyBinding newBinding,
		out string? conflictLabel)
	{
		conflictLabel = null;
		if (!TryFindSpec(action, out var targetSpec))
			return false;

		if (!newBinding.IsEmpty && !AllowsDevice(targetSpec.CapturePolicy, newBinding.Device))
			return false;

		if (IsReserved(targetSpec.Context, newBinding))
			return false;

		if (!secondarySlot && newBinding.IsEmpty)
			return false;

		if (!newBinding.IsEmpty
			&& draft.TryGetValue(action, out var existing))
		{
			var other = secondarySlot ? existing.Primary : existing.Secondary ?? KeyBinding.Empty;
			if (!other.IsEmpty && ChordEquals(other, newBinding))
				return false;
		}

		if (TryFindConflict(draft, targetSpec, action, newBinding, out conflictLabel))
			return false;

		return true;
	}

	public static IReadOnlyDictionary<StringName, BindingPair> WithAssignment(
		IReadOnlyDictionary<StringName, BindingPair> draft,
		StringName action,
		bool secondarySlot,
		KeyBinding newBinding)
	{
		var copy = Copy(draft);
		if (!copy.TryGetValue(action, out var pair))
			return copy;

		copy[action] = secondarySlot
			? new BindingPair(pair.Primary, newBinding.IsEmpty ? null : newBinding)
			: new BindingPair(newBinding, pair.Secondary);

		return copy;
	}

	public static bool TryParseSlot(string? encoded, out KeyBinding binding)
	{
		binding = KeyBinding.Empty;
		if (string.IsNullOrWhiteSpace(encoded))
			return false;

		var parts = encoded.Split(':');
		if (parts.Length != 3)
			return false;

		if (parts[0] == "mouse")
		{
			if (!long.TryParse(parts[1], out var buttonCode))
				return false;

			var button = (MouseButton)buttonCode;
			if (!Enum.IsDefined(typeof(MouseButton), button) || button == MouseButton.None)
				return false;

			binding = KeyBinding.Mouse(button);
			return true;
		}

		if (!TryParseKind(parts[0], out var kind))
			return false;

		if (!long.TryParse(parts[1], out var keyCode) || keyCode <= 0)
			return false;

		var key = (Key)keyCode;
		if (!Enum.IsDefined(typeof(Key), key))
			return false;
		if (key == Key.None || IsModifierOnlyKey(key))
			return false;

		if (!TryParseModifier(parts[2], out var modifier))
			return false;

		binding = new KeyBinding(kind, key, modifier);
		return true;
	}

	public static string EncodeSlot(KeyBinding binding)
	{
		if (binding.Device == BindingDevice.Mouse)
			return $"mouse:{(long)binding.MouseButton}:none";

		var kind = binding.Kind == KeyKind.Physical ? "physical" : "logical";
		var modifier = EncodeModifier(binding.Modifier);
		return $"{kind}:{(long)binding.Key}:{modifier}";
	}

	public static void WriteBindingsSection(ConfigFile config, IReadOnlyDictionary<StringName, BindingPair> bindings)
	{
		ArgumentNullException.ThrowIfNull(config);
		config.SetValue("bindings", "version", BindingsVersion);

		foreach (var spec in Specs)
		{
			var pair = bindings[spec.Action];
			config.SetValue("bindings", $"{spec.Action}.primary", EncodeSlot(pair.Primary));
			if (pair.Secondary is { } secondary && !secondary.IsEmpty)
				config.SetValue("bindings", $"{spec.Action}.secondary", EncodeSlot(secondary));
			else if (config.HasSectionKey("bindings", $"{spec.Action}.secondary"))
				config.EraseSectionKey("bindings", $"{spec.Action}.secondary");
		}
	}

	public static IReadOnlyDictionary<StringName, BindingPair> OverlaySavedBindings(ConfigFile config)
	{
		var result = Copy(DefaultBindings());
		foreach (var spec in Specs)
		{
			var primaryKey = $"{spec.Action}.primary";
			if (config.HasSectionKey("bindings", primaryKey)
				&& TryParseSlot(config.GetValue("bindings", primaryKey).AsString(), out var primary)
				&& AllowsDevice(spec.CapturePolicy, primary.Device))
				result[spec.Action] = result[spec.Action] with { Primary = primary };

			var secondaryKey = $"{spec.Action}.secondary";
			if (config.HasSectionKey("bindings", secondaryKey)
				&& TryParseSlot(config.GetValue("bindings", secondaryKey).AsString(), out var secondary)
				&& AllowsDevice(spec.CapturePolicy, secondary.Device))
				result[spec.Action] = result[spec.Action] with { Secondary = secondary };
		}

		return result;
	}

	internal static InputEvent ToInputEvent(KeyBinding binding)
	{
		if (binding.Device == BindingDevice.Mouse)
			return new InputEventMouseButton { ButtonIndex = binding.MouseButton };

		var result = new InputEventKey();

		if (binding.Kind == KeyKind.Physical)
			result.PhysicalKeycode = binding.Key;
		else
			result.Keycode = binding.Key;

		switch (binding.Modifier)
		{
			case ModifierKind.Primary:
				result.CommandOrControlAutoremap = true;
				break;
			case ModifierKind.Ctrl:
				result.CtrlPressed = true;
				break;
			case ModifierKind.Meta:
				result.MetaPressed = true;
				break;
			case ModifierKind.Shift:
				result.ShiftPressed = true;
				break;
			case ModifierKind.Alt:
				result.AltPressed = true;
				break;
		}

		return result;
	}

	private static IReadOnlyList<BindingSpec> BuildSpecs() =>
	[
		Map("map_pan_up", "Pan up", Phy(Key.W), Phy(Key.Up)),
		Map("map_pan_down", "Pan down", Phy(Key.S), Phy(Key.Down)),
		Map("map_pan_left", "Pan left", Phy(Key.A), Phy(Key.Left)),
		Map("map_pan_right", "Pan right", Phy(Key.D), Phy(Key.Right)),
		Map("map_pause", "Pause / resume", Log(Key.Space)),
		Map("map_step", "Step", Log(Key.Period)),
		Map("map_speed_up", "Speed up", Log(Key.Bracketright)),
		Map("map_speed_down", "Speed down", Log(Key.Bracketleft)),

		BattleKey("battle_pan_up", "Pan up", Phy(Key.W)),
		BattleKey("battle_pan_down", "Pan down", Phy(Key.S)),
		BattleKey("battle_pan_left", "Pan left", Phy(Key.A)),
		BattleKey("battle_pan_right", "Pan right", Phy(Key.D)),
		BattleKey("battle_move_mode", "Move mode", Phy(Key.Key1)),
		BattleKey("battle_ability_2", "Ability slot 2", Phy(Key.Key2)),
		BattleKey("battle_ability_3", "Ability slot 3", Phy(Key.Key3)),
		BattleKey("battle_ability_4", "Ability slot 4", Phy(Key.Key4)),
		BattleKey("battle_end_turn", "End turn", Log(Key.Space)),
		BattleKey("battle_focus", "Focus camera", Log(Key.F)),
		BattleKey("battle_undo", "Undo", new(KeyKind.Logical, Key.Z, ModifierKind.Primary)),

		BattleAny(
			"battle_zoom_in",
			"Zoom in",
			KeyBinding.Mouse(MouseButton.WheelUp),
			Log(Key.Equal),
			conflictGroup: "battle_zoom"),
		BattleAny(
			"battle_zoom_out",
			"Zoom out",
			KeyBinding.Mouse(MouseButton.WheelDown),
			Log(Key.Minus),
			conflictGroup: "battle_zoom"),
		BattleMouse(
			"battle_camera_orbit",
			"Orbit camera (hold)",
			KeyBinding.Mouse(MouseButton.Right),
			conflictGroup: "battle_camera_hold"),
		BattleMouse(
			"battle_camera_pan",
			"Pan camera (hold)",
			KeyBinding.Mouse(MouseButton.Middle),
			conflictGroup: "battle_camera_hold"),
		BattleMouse(
			"battle_primary_click",
			"Select / activate",
			KeyBinding.Mouse(MouseButton.Left),
			conflictGroup: "battle_lmb_press"),
		BattleMouse(
			"battle_move_confirm",
			"Confirm move (release)",
			KeyBinding.Mouse(MouseButton.Left),
			conflictGroup: "battle_lmb_release"),
		BattleMouse(
			"battle_move_cancel",
			"Cancel move",
			KeyBinding.Mouse(MouseButton.Right),
			conflictGroup: "battle_move_cancel"),
		BattleAny(
			"battle_roll_clockwise",
			"Roll clockwise",
			KeyBinding.Mouse(MouseButton.WheelUp),
			conflictGroup: "battle_move_roll"),
		BattleAny(
			"battle_roll_counterclockwise",
			"Roll counter-clockwise",
			KeyBinding.Mouse(MouseButton.WheelDown),
			conflictGroup: "battle_move_roll"),
	];

	private static BindingSpec Map(
		string action,
		string label,
		KeyBinding primary,
		KeyBinding? secondary = null) =>
		new(action, BindingContext.WorldMap, label, primary, secondary);

	private static BindingSpec BattleKey(
		string action,
		string label,
		KeyBinding primary,
		KeyBinding? secondary = null) =>
		new(action, BindingContext.Battle, label, primary, secondary);

	private static BindingSpec BattleMouse(
		string action,
		string label,
		KeyBinding primary,
		KeyBinding? secondary = null,
		string? conflictGroup = null) =>
		new(
			action,
			BindingContext.Battle,
			label,
			primary,
			secondary,
			BindingCapturePolicy.MouseOnly,
			conflictGroup);

	private static BindingSpec BattleAny(
		string action,
		string label,
		KeyBinding primary,
		KeyBinding? secondary = null,
		string? conflictGroup = null) =>
		new(
			action,
			BindingContext.Battle,
			label,
			primary,
			secondary,
			BindingCapturePolicy.Any,
			conflictGroup);

	private static KeyBinding Phy(Key key) => new(KeyKind.Physical, key);
	private static KeyBinding Log(Key key) => new(KeyKind.Logical, key);

	private static Dictionary<StringName, BindingPair> Copy(
		IReadOnlyDictionary<StringName, BindingPair> source)
	{
		var copy = new Dictionary<StringName, BindingPair>(source.Count);
		foreach (var (action, pair) in source)
			copy[action] = pair;
		return copy;
	}

	private static bool TryFindConflict(
		IReadOnlyDictionary<StringName, BindingPair> draft,
		BindingSpec targetSpec,
		StringName skipAction,
		KeyBinding candidate,
		out string? conflictLabel)
	{
		conflictLabel = null;
		if (candidate.IsEmpty)
			return false;

		var targetGroup = ConflictGroupOf(targetSpec);

		foreach (var spec in Specs)
		{
			if (spec.Context != targetSpec.Context || spec.Action == skipAction)
				continue;

			if (ConflictGroupOf(spec) != targetGroup)
				continue;

			if (!draft.TryGetValue(spec.Action, out var pair))
				continue;

			if (ChordEquals(pair.Primary, candidate))
			{
				conflictLabel = spec.Label;
				return true;
			}

			if (pair.Secondary is { } secondary
				&& !secondary.IsEmpty
				&& ChordEquals(secondary, candidate))
			{
				conflictLabel = spec.Label;
				return true;
			}
		}

		return false;
	}

	private static string ConflictGroupOf(BindingSpec spec) =>
		spec.ConflictGroup ?? spec.Action;

	private static bool ChordEquals(KeyBinding a, KeyBinding b)
	{
		if (a.Device != b.Device)
			return false;

		if (a.Device == BindingDevice.Mouse)
			return a.MouseButton == b.MouseButton;

		if (a.Kind != b.Kind || a.Key != b.Key)
			return false;

		return EffectiveModifier(a.Modifier) == EffectiveModifier(b.Modifier);
	}

	private static ModifierKind EffectiveModifier(ModifierKind modifier) =>
		modifier == ModifierKind.Primary
			? OperatingSystem.IsMacOS() ? ModifierKind.Meta : ModifierKind.Ctrl
			: modifier;

	private static ModifierKind ReadModifier(InputEventKey keyEvent)
	{
		if (keyEvent.CommandOrControlAutoremap)
			return ModifierKind.Primary;

		var count = 0;
		ModifierKind modifier = ModifierKind.None;
		if (keyEvent.CtrlPressed)
		{
			modifier = ModifierKind.Ctrl;
			count++;
		}

		if (keyEvent.MetaPressed)
		{
			modifier = ModifierKind.Meta;
			count++;
		}

		if (keyEvent.ShiftPressed)
		{
			modifier = ModifierKind.Shift;
			count++;
		}

		if (keyEvent.AltPressed)
		{
			modifier = ModifierKind.Alt;
			count++;
		}

		return count == 1 ? modifier : ModifierKind.None;
	}

	private static bool PreferPhysicalKind(Key key) =>
		key is >= Key.A and <= Key.Z
			or >= Key.Key0 and <= Key.Key9
			or Key.Up or Key.Down or Key.Left or Key.Right;

	private static bool IsModifierOnlyKey(Key key) =>
		key is Key.Ctrl
			or Key.Shift
			or Key.Alt
			or Key.Meta
			or Key.Capslock
			or Key.Numlock;

	private static bool TryParseKind(string token, out KeyKind kind)
	{
		switch (token)
		{
			case "physical":
				kind = KeyKind.Physical;
				return true;
			case "logical":
				kind = KeyKind.Logical;
				return true;
			default:
				kind = default;
				return false;
		}
	}

	private static bool TryParseModifier(string token, out ModifierKind modifier)
	{
		switch (token)
		{
			case "none":
				modifier = ModifierKind.None;
				return true;
			case "ctrl":
				modifier = ModifierKind.Ctrl;
				return true;
			case "meta":
				modifier = ModifierKind.Meta;
				return true;
			case "shift":
				modifier = ModifierKind.Shift;
				return true;
			case "alt":
				modifier = ModifierKind.Alt;
				return true;
			case "primary":
				modifier = ModifierKind.Primary;
				return true;
			default:
				modifier = default;
				return false;
		}
	}

	private static string EncodeModifier(ModifierKind modifier) =>
		modifier switch
		{
			ModifierKind.None => "none",
			ModifierKind.Ctrl => "ctrl",
			ModifierKind.Meta => "meta",
			ModifierKind.Shift => "shift",
			ModifierKind.Alt => "alt",
			ModifierKind.Primary => "primary",
			_ => "none",
		};

	private static string FormatBinding(KeyBinding binding)
	{
		if (binding.IsEmpty)
			return "—";

		if (binding.Device == BindingDevice.Mouse)
			return FormatMouseButton(binding.MouseButton);

		var builder = new StringBuilder();
		switch (binding.Modifier)
		{
			case ModifierKind.Primary:
				builder.Append(InputShortcutText.PrimaryModifierFor(OperatingSystem.IsMacOS()));
				builder.Append('+');
				break;
			case ModifierKind.Ctrl:
				builder.Append("Ctrl+");
				break;
			case ModifierKind.Meta:
				builder.Append("Cmd+");
				break;
			case ModifierKind.Shift:
				builder.Append("Shift+");
				break;
			case ModifierKind.Alt:
				builder.Append("Alt+");
				break;
		}

		builder.Append(FormatKeyboardKey(binding));
		return builder.ToString();
	}

	private static string FormatMouseButton(MouseButton button) =>
		button switch
		{
			MouseButton.Left => "LMB",
			MouseButton.Right => "RMB",
			MouseButton.Middle => "MMB",
			MouseButton.WheelUp => "Wh↑",
			MouseButton.WheelDown => "Wh↓",
			MouseButton.WheelLeft => "Wheel left",
			MouseButton.WheelRight => "Wheel right",
			MouseButton.Xbutton1 => "M4",
			MouseButton.Xbutton2 => "M5",
			_ => button.ToString(),
		};

	private static string FormatKeyboardKey(KeyBinding binding)
	{
		switch (binding.Key)
		{
			case Key.Equal:
				return "+";
			case Key.Minus:
				return "-";
			case Key.KpAdd:
				return "Num +";
			case Key.KpSubtract:
				return "Num -";
		}

		if (binding.Kind == KeyKind.Physical)
		{
			var label = OS.GetKeycodeString(
				DisplayServer.KeyboardGetKeycodeFromPhysical(binding.Key));
			if (!string.IsNullOrEmpty(label))
				return FriendlyKeyLabel(label, binding.Key);
		}

		var logical = OS.GetKeycodeString(binding.Key);
		return FriendlyKeyLabel(
			string.IsNullOrEmpty(logical) ? binding.Key.ToString() : logical,
			binding.Key);
	}

	private static string FriendlyKeyLabel(string osLabel, Key key)
	{
		if (key == Key.Equal || osLabel.Equals("Equal", StringComparison.OrdinalIgnoreCase))
			return "+";
		if (key == Key.Minus || osLabel.Equals("Minus", StringComparison.OrdinalIgnoreCase))
			return "-";
		return osLabel;
	}
}
