using Godot;
using GrimSpace.Application;

namespace GrimSpace.Presentation.Menu;

public partial class KeyBindingsTab : Control
{
	private readonly record struct BindingSubsection(string Title, string[] ActionIds);

	private static readonly BindingSubsection[] BattleSubsections =
	[
		new(
			"Camera",
			[
				"battle_pan_up",
				"battle_pan_down",
				"battle_pan_left",
				"battle_pan_right",
				"battle_zoom_in",
				"battle_zoom_out",
				"battle_camera_orbit",
				"battle_camera_pan",
				"battle_focus",
			]),
		new(
			"Move placement",
			[
				"battle_move_confirm",
				"battle_move_cancel",
				"battle_roll_clockwise",
				"battle_roll_counterclockwise",
			]),
		new(
			"Pointer & shortcuts",
			[
				"battle_primary_click",
				"battle_move_mode",
				"battle_ability_2",
				"battle_ability_3",
				"battle_ability_4",
			]),
		new("Turn", ["battle_end_turn", "battle_undo"]),
	];

	private static readonly BindingSubsection[] WorldMapSubsections =
	[
		new(
			"Camera",
			[
				"map_pan_up",
				"map_pan_down",
				"map_pan_left",
				"map_pan_right",
				"map_zoom_in",
				"map_zoom_out",
			]),
		new(
			"Time",
			["map_pause", "map_speed_up", "map_speed_down"]),
	];

	private const float ActionColumnMinWidth = 200f;
	private const float KeyColumnWidth = 112f;
	private const int ContextTitleFontSize = 44;
	private const int SubsectionTitleFontSize = 36;

	private VBoxContainer _rowList = null!;
	private Label _statusLabel = null!;
	private KeyCaptureControl _capture = null!;
	private StringName _capturingAction = "";
	private bool _capturingSecondary;

	private Dictionary<StringName, BindingPair> _draft = [];

	public override void _Ready()
	{
		SizeFlagsHorizontal = SizeFlags.ExpandFill;
		SizeFlagsVertical = SizeFlags.ExpandFill;
		if (_draft.Count == 0)
			_draft = CopyDraft(GameInputBindings.DefaultBindings());
		BuildChrome();
		RebuildRows();
	}

	public void LoadDraft(IReadOnlyDictionary<StringName, BindingPair> bindings)
	{
		_draft = CopyDraft(bindings);
		_statusLabel.Text = string.Empty;
		RebuildRows();
	}

	public void ShowStatus(string message) => _statusLabel.Text = message;

	public void ResetToDefaults()
	{
		_draft = CopyDraft(GameInputBindings.DefaultBindings());
		ShowStatus("Reset to defaults (Apply to save).");
		RebuildRows();
	}

	public bool TryGetCommittedBindings(out IReadOnlyDictionary<StringName, BindingPair> bindings, out string? error)
	{
		bindings = _draft;
		error = null;
		foreach (var spec in GameInputBindings.Specs)
		{
			if (_draft[spec.Action].Primary.IsEmpty)
			{
				error = $"{spec.Label} needs a primary binding.";
				return false;
			}
		}

		return true;
	}

	private void BuildChrome()
	{
		var root = new VBoxContainer();
		root.AddThemeConstantOverride("separation", 8);
		root.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
		AddChild(root);

		var scroll = new ScrollContainer
		{
			SizeFlagsVertical = SizeFlags.ExpandFill,
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
		};
		root.AddChild(scroll);

		_rowList = new VBoxContainer
		{
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
		};
		_rowList.AddThemeConstantOverride("separation", 10);
		scroll.AddChild(_rowList);

		_statusLabel = new Label
		{
			AutowrapMode = TextServer.AutowrapMode.WordSmart,
			ThemeTypeVariation = "BodyLabel",
		};
		root.AddChild(_statusLabel);

		_capture = new KeyCaptureControl
		{
			FocusMode = FocusModeEnum.All,
			CustomMinimumSize = new Vector2(0, 1),
		};
		_capture.BindingCaptured += OnBindingCaptured;
		_capture.CaptureCancelled += OnCaptureCancelled;
		root.AddChild(_capture);
	}

	private void RebuildRows()
	{
		foreach (var child in _rowList.GetChildren())
			child.QueueFree();

		AddContextSection("Battle", BattleSubsections, first: true);
		AddContextSection("World Map", WorldMapSubsections, first: false);
	}

	private void AddContextSection(string title, BindingSubsection[] subsections, bool first)
	{
		if (!first)
		{
			_rowList.AddChild(new Control
			{
				CustomMinimumSize = new Vector2(0, 16),
			});
		}

		_rowList.AddChild(CreateContextTitle(title));

		foreach (var subsection in subsections)
			AddSubsection(subsection);
	}

	private static Label CreateContextTitle(string title)
	{
		var label = new Label
		{
			Text = title,
			ThemeTypeVariation = "SectionHeadingLabel",
		};
		label.AddThemeFontSizeOverride("font_size", ContextTitleFontSize);
		return label;
	}

	private static RichTextLabel CreateSubsectionTitle(string title)
	{
		var label = new RichTextLabel
		{
			BbcodeEnabled = true,
			Text = $"[u]{title}[/u]",
			FitContent = true,
			ScrollActive = false,
			MouseFilter = MouseFilterEnum.Ignore,
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
		};
		label.AddThemeFontSizeOverride("normal_font_size", SubsectionTitleFontSize);
		label.AddThemeColorOverride("default_color", new Color(0.55f, 0.62f, 0.7f));
		return label;
	}

	private void AddSubsection(BindingSubsection subsection)
	{
		_rowList.AddChild(new Control { CustomMinimumSize = new Vector2(0, 8) });

		_rowList.AddChild(CreateSubsectionTitle(subsection.Title));

		var rowsBlock = new MarginContainer();
		rowsBlock.AddThemeConstantOverride("margin_left", 16);
		_rowList.AddChild(rowsBlock);

		var rows = new VBoxContainer();
		rows.AddThemeConstantOverride("separation", 4);
		rowsBlock.AddChild(rows);

		rows.AddChild(CreateColumnHeaderRow());

		foreach (var actionId in subsection.ActionIds)
		{
			if (!GameInputBindings.TryFindSpec(actionId, out var spec))
				continue;

			rows.AddChild(CreateBindingRow(spec));
		}
	}

	private static Control CreateColumnHeaderRow()
	{
		var row = new HBoxContainer();
		row.AddThemeConstantOverride("separation", 8);

		var actionSpacer = new Control
		{
			CustomMinimumSize = new Vector2(ActionColumnMinWidth, 0),
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
		};
		row.AddChild(actionSpacer);

		row.AddChild(CreateColumnTitle("KEY"));
		row.AddChild(CreateColumnTitle("Alt"));

		return row;
	}

	private static Label CreateColumnTitle(string text) =>
		new()
		{
			Text = text,
			CustomMinimumSize = new Vector2(KeyColumnWidth, 0),
			HorizontalAlignment = HorizontalAlignment.Center,
			ThemeTypeVariation = "MetadataLabel",
		};

	private Control CreateBindingRow(BindingSpec spec)
	{
		var pair = _draft[spec.Action];
		var row = new HBoxContainer();
		row.AddThemeConstantOverride("separation", 8);

		row.AddChild(new Label
		{
			Text = spec.Label,
			CustomMinimumSize = new Vector2(ActionColumnMinWidth, 0),
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
			ThemeTypeVariation = "BodyLabel",
		});

		row.AddChild(CreateBindingButton(spec.Action, false, pair.Primary));
		row.AddChild(CreateBindingButton(
			spec.Action,
			true,
			pair.Secondary ?? KeyBinding.Empty));

		return row;
	}

	private Button CreateBindingButton(StringName action, bool secondary, KeyBinding binding)
	{
		var text = binding.IsEmpty ? "—" : GameInputBindings.LabelForBinding(binding);
		var button = new Button
		{
			Text = text,
			ClipText = true,
			CustomMinimumSize = new Vector2(KeyColumnWidth, 32),
			ThemeTypeVariation = secondary ? "SecondaryButton" : "PrimaryButton",
		};
		button.Pressed += () => BeginCapture(action, secondary, button);
		return button;
	}

	private void BeginCapture(StringName action, bool secondary, Button button)
	{
		_capturingAction = action;
		_capturingSecondary = secondary;

		if (!GameInputBindings.TryFindSpec(action, out var spec))
			return;

		_statusLabel.Text = CaptureHint(spec.CapturePolicy, secondary);
		_capture.BeginCapture(action, secondary, button);
	}

	private static string CaptureHint(BindingCapturePolicy policy, bool secondary) =>
		policy switch
		{
			BindingCapturePolicy.MouseOnly => secondary
				? "Click a mouse button or wheel (RMB clears). Escape cancels."
				: "Click a mouse button or wheel. Escape cancels.",
			BindingCapturePolicy.Any => secondary
				? "Press a key or click mouse (Backspace / RMB clears). Escape cancels."
				: "Press a key or click mouse. Escape cancels.",
			_ => secondary
				? "Press a key for Alt (Backspace clears). Escape cancels."
				: "Press a key. Escape cancels.",
		};

	private void OnBindingCaptured(KeyBinding binding, bool secondarySlot)
	{
		var action = _capturingAction;
		if (action.IsEmpty)
			return;

		if (!GameInputBindings.TryAssign(_draft, action, secondarySlot, binding, out _))
			return;

		_draft = CopyDraft(GameInputBindings.WithAssignment(_draft, action, secondarySlot, binding));
		_statusLabel.Text = string.Empty;
		RebuildRows();
	}

	private void OnCaptureCancelled() =>
		_statusLabel.Text = string.Empty;

	private static Dictionary<StringName, BindingPair> CopyDraft(
		IReadOnlyDictionary<StringName, BindingPair> source)
	{
		var copy = new Dictionary<StringName, BindingPair>(source.Count);
		foreach (var (action, pair) in source)
			copy[action] = pair;
		return copy;
	}
}
