using Godot;

namespace GrimSpace.Battle.Presentation.Ui;

/// <summary>Focus/undo controls placed beside the action bar (not inside it).</summary>
public sealed partial class UtilityBar : PanelContainer
{
	public event Action? FocusRequested;
	public event Action? UndoRequested;

	private const int SlotSize = 64;
	private const float IconPx = 40f;
	private const float SvgSourceSize = 512f;

	private static readonly Color Accent = new(0.55f, 0.78f, 1f);

	private Button _focusButton = null!;
	private Button _undoButton = null!;

	public UtilityBar()
	{
		MouseFilter = MouseFilterEnum.Stop;
		AddThemeStyleboxOverride("panel", MakeStyle(
			new Color(0.08f, 0.1f, 0.14f, 0.92f),
			new Color(0.35f, 0.55f, 0.85f, 0.75f),
			2,
			8));
		Build();
	}

	public void Configure(bool focusAvailable, bool undoAvailable)
	{
		_focusButton.Disabled = !focusAvailable;
		_undoButton.Disabled = !undoAvailable;
	}

	public bool TryFocus()
	{
		if (_focusButton.Disabled)
			return false;
		FocusRequested?.Invoke();
		return true;
	}

	public bool TryUndo()
	{
		if (_undoButton.Disabled)
			return false;
		UndoRequested?.Invoke();
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
		col.AddThemeConstantOverride("separation", 8);
		pad.AddChild(col);

		_focusButton = CreateSlot(
			hotkey: "F",
			tooltip: "Snap the camera to your active ship.",
			iconPath: "res://assets/ui/abilities/focus.svg",
			onPressed: () => FocusRequested?.Invoke());
		_undoButton = CreateSlot(
			hotkey: "⌘Z",
			tooltip: "Undo your last action this turn.\n(Ctrl/Cmd+Z)",
			iconPath: "res://assets/ui/abilities/undo.svg",
			onPressed: () => UndoRequested?.Invoke());

		col.AddChild(_focusButton);
		col.AddChild(_undoButton);
	}

	private static Button CreateSlot(string hotkey, string tooltip, string iconPath, Action onPressed)
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
		button.Pressed += onPressed;
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
