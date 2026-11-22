using Godot;
using GrimSpace.Application;
using GrimSpace.Battle.Player;
using GrimSpace.Components;
using GrimSpace.Ui.Components;

namespace GrimSpace.Battle.Presentation.Ui;

public sealed partial class BattleControlHints : PanelContainer
{
	private readonly HBoxContainer _hints;
	private readonly Font _bindingFont = GD.Load<Font>("res://assets/ui/fonts/VT323-Regular.ttf")
		?? throw new InvalidOperationException("Could not load the control hint binding font.");
	private IReadOnlyDictionary<StringName, BindingPair>? _bindings;
	private (EPlayerMode Mode, bool CanAct, bool CanPlace, bool Pose, bool Isolated)? _context;
	private PresentationFrame? _frame;

	public BattleControlHints()
	{
		Name = "ControlHints";
		SizeFlagsVertical = SizeFlags.ShrinkCenter;
		MouseFilter = MouseFilterEnum.Ignore;
		Visible = false;
		AddThemeStyleboxOverride("panel", new StyleBoxFlat
		{
			BgColor = Colors.Transparent,
			ContentMarginLeft = 28f,
			ContentMarginRight = 12f,
			ContentMarginTop = 8f,
			ContentMarginBottom = 8f,
		});
		_hints = new HBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
		_hints.AddThemeConstantOverride("separation", 10);
		AddChild(_hints);
	}

	public void Apply(PresentationFrame frame)
	{
		_frame = frame;
		var context = (Mode: frame.Mode, CanAct: frame.CanAct && !frame.IsInspecting,
			CanPlace: frame.CanAct && !frame.IsInspecting && frame.ShowMovePreview,
			Pose: frame.IsMovePoseSelectionActive, Isolated: frame.IsolatedMoveCells is not null);
		if (_context == context && ReferenceEquals(_bindings, GameInputBindings.Current))
			return;
		_context = context;
		_bindings = GameInputBindings.Current;
		foreach (var child in _hints.GetChildren())
		{
			_hints.RemoveChild(child);
			child.QueueFree();
		}

		if (frame.CanAct && !frame.IsInspecting)
			AddHint(frame.Mode == EPlayerMode.Move
				? frame.IsMovePoseSelectionActive ? "Confirm heading" : "Move"
				: "Select target", false, "battle_primary_click");
		AddHint("Orbit", true, "battle_camera_orbit");
		AddHint("Pan", false, "battle_pan_up", "battle_pan_left", "battle_pan_down", "battle_pan_right");
		AddHint("Free camera", true, "battle_camera_pan");
		if (frame.CanAct && frame.IsMovePoseSelectionActive)
			AddHint("Roll", false, "battle_roll_clockwise", "battle_roll_counterclockwise");
		else
			AddHint("Zoom", false, "battle_zoom_in", "battle_zoom_out");
		if (context.CanPlace && (!context.Pose || context.Isolated))
			AddHint("Move Pick Hints", true, "battle_isolate_movement");
	}

	public override void _Process(double delta)
	{
		if (_frame is { } frame && !ReferenceEquals(_bindings, GameInputBindings.Current))
			Apply(frame);
	}

	private void AddHint(string title, bool hold, params StringName[] actions)
	{
		var hint = new VBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
		hint.AddThemeConstantOverride("separation", 4);
		var label = new Label
		{
			Text = title,
			HorizontalAlignment = HorizontalAlignment.Center,
			MouseFilter = MouseFilterEnum.Ignore,
		};
		label.AddThemeFontSizeOverride("font_size", 14);
		label.AddThemeColorOverride("font_color", HudPalettes.BlueCyan.Secondary);
		hint.AddChild(label);
		var inputs = new HBoxContainer
		{
			CustomMinimumSize = new Vector2(0f, 30f),
			Alignment = BoxContainer.AlignmentMode.Center,
			MouseFilter = MouseFilterEnum.Ignore,
		};
		inputs.AddThemeConstantOverride("separation", 3);
		var shown = new HashSet<KeyBinding>();
		var hasMouseIcon = false;
		foreach (var action in actions)
		{
			var binding = GameInputBindings.Current[action].Primary;
			if (binding.Device == BindingDevice.Mouse && binding.MouseButton == MouseButton.WheelDown)
				binding = binding with { MouseButton = MouseButton.WheelUp };
			if (!shown.Add(binding))
				continue;
			if (binding.Device == BindingDevice.Mouse && UiKeycap.MouseIconPath(binding.MouseButton) is not null)
			{
				hasMouseIcon = true;
				inputs.AddChild(UiKeycap.ForBinding(binding));
			}
			else
			{
				var key = new Label
				{
					Text = GameInputBindings.LabelForBinding(binding),
					CustomMinimumSize = new Vector2(12f, 26f),
					VerticalAlignment = VerticalAlignment.Center,
					HorizontalAlignment = HorizontalAlignment.Center,
					MouseFilter = MouseFilterEnum.Ignore,
				};
				key.AddThemeFontOverride("font", _bindingFont);
				key.AddThemeFontSizeOverride("font_size", 20);
				key.AddThemeColorOverride("font_color", Colors.White);
				inputs.AddChild(key);
			}
		}
		hint.AddChild(inputs);
		if (hold)
		{
			var held = new Label
			{
				Text = hasMouseIcon ? "hold" : "+ hold",
				HorizontalAlignment = HorizontalAlignment.Center,
				MouseFilter = MouseFilterEnum.Ignore,
			};
			held.AddThemeFontOverride("font", _bindingFont);
			held.AddThemeFontSizeOverride("font_size", 20);
			held.AddThemeColorOverride("font_color", Colors.White);
			if (hasMouseIcon)
				hint.AddChild(held);
			else
				inputs.AddChild(held);
		}
		_hints.AddChild(hint);
	}
}
