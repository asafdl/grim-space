using Godot;

namespace GrimSpace.Battle.Presentation.Ui;

public sealed partial class ActionBar : HBoxContainer
{
	public event Action<EPlayerMode>? ModeChanged;
	public event Action? EndTurnRequested;

	private const int SlotSize = 64;
	private const float IconPx = 40f;
	private const float SvgSourceSize = 512f;

	private readonly ButtonGroup _modeGroup = new();
	private Button _moveButton = null!;
	private Button _flakButton = null!;
	private Button _railgunButton = null!;
	private Button _torpedoButton = null!;
	private Button _endTurnButton = null!;

	public ActionBar()
	{
		MouseFilter = MouseFilterEnum.Ignore;
		Alignment = AlignmentMode.Center;
		AddThemeConstantOverride("separation", 14);
		Build();
	}

	public void SetMode(EPlayerMode mode)
	{
		_moveButton.SetBlockSignals(true);
		_flakButton.SetBlockSignals(true);
		_railgunButton.SetBlockSignals(true);
		_torpedoButton.SetBlockSignals(true);

		_moveButton.ButtonPressed = mode == EPlayerMode.Move;
		_flakButton.ButtonPressed = mode == EPlayerMode.Flak;
		_railgunButton.ButtonPressed = mode == EPlayerMode.Railgun;
		_torpedoButton.ButtonPressed = mode == EPlayerMode.Torpedo;

		_moveButton.SetBlockSignals(false);
		_flakButton.SetBlockSignals(false);
		_railgunButton.SetBlockSignals(false);
		_torpedoButton.SetBlockSignals(false);
	}

	public void Configure(bool flakAvailable, bool railgunAvailable, bool torpedoAvailable, bool canAct)
	{
		_flakButton.Disabled = !canAct || !flakAvailable;
		_railgunButton.Disabled = !canAct || !railgunAvailable;
		_torpedoButton.Disabled = !canAct || !torpedoAvailable;
		_moveButton.Disabled = !canAct;
		_endTurnButton.Disabled = !canAct;
	}

	/// <summary>Activates ability slot 1–4 if enabled. Returns false if unbound or disabled.</summary>
	public bool TryActivateHotkey(int slot)
	{
		var button = slot switch
		{
			1 => _moveButton,
			2 => _flakButton,
			3 => _railgunButton,
			4 => _torpedoButton,
			_ => null,
		};
		if (button is null || button.Disabled)
			return false;

		button.ButtonPressed = true;
		return true;
	}

	private void Build()
	{
		var abilityPanel = CreatePanel(new Color(0.08f, 0.1f, 0.14f, 0.92f), new Color(0.35f, 0.55f, 0.85f, 0.75f));
		AddChild(abilityPanel);

		var abilityPad = CreatePad();
		abilityPanel.AddChild(abilityPad);

		var abilityRow = new HBoxContainer();
		abilityRow.AddThemeConstantOverride("separation", 8);
		abilityPad.AddChild(abilityRow);

		var abilityAccent = new Color(0.55f, 0.78f, 1f);
		_moveButton = CreateAbilitySlot(
			hotkey: "1",
			tooltip: "Move",
			iconPath: "res://assets/ui/abilities/move.svg",
			accent: abilityAccent,
			onPressed: () => ModeChanged?.Invoke(EPlayerMode.Move));
		_flakButton = CreateAbilitySlot(
			hotkey: "2",
			tooltip: "Flak",
			iconPath: "res://assets/ui/abilities/flak.svg",
			accent: abilityAccent,
			onPressed: () => ModeChanged?.Invoke(EPlayerMode.Flak));
		_railgunButton = CreateAbilitySlot(
			hotkey: "3",
			tooltip: "Railgun",
			iconPath: "res://assets/ui/abilities/railgun.svg",
			accent: abilityAccent,
			onPressed: () => ModeChanged?.Invoke(EPlayerMode.Railgun));
		_torpedoButton = CreateAbilitySlot(
			hotkey: "4",
			tooltip: "Torpedo",
			iconPath: "res://assets/ui/abilities/torpedo.svg",
			accent: abilityAccent,
			onPressed: () => ModeChanged?.Invoke(EPlayerMode.Torpedo));

		abilityRow.AddChild(_moveButton);
		abilityRow.AddChild(_flakButton);
		abilityRow.AddChild(_railgunButton);
		abilityRow.AddChild(_torpedoButton);

		var endPanel = CreatePanel(new Color(0.16f, 0.1f, 0.05f, 0.94f), new Color(0.95f, 0.7f, 0.25f, 0.9f));
		AddChild(endPanel);

		var endPad = CreatePad();
		endPanel.AddChild(endPad);

		_endTurnButton = CreateEndTurnButton();
		endPad.AddChild(_endTurnButton);

		_moveButton.ButtonPressed = true;
	}

	private Button CreateAbilitySlot(string hotkey, string tooltip, string iconPath, Color accent, Action onPressed)
	{
		var button = new Button
		{
			ToggleMode = true,
			ButtonGroup = _modeGroup,
			CustomMinimumSize = new Vector2(SlotSize, SlotSize),
			Icon = LoadSvgIcon(iconPath, accent),
			ExpandIcon = false,
			TooltipText = tooltip,
			FocusMode = FocusModeEnum.None,
			ClipContents = false,
		};

		ApplySlotStyles(button, accent);
		AddHotkeyBadge(button, hotkey);

		button.Toggled += pressed =>
		{
			if (pressed)
				onPressed();
		};
		return button;
	}

	private Button CreateEndTurnButton()
	{
		var button = new Button
		{
			Text = "End Turn",
			CustomMinimumSize = new Vector2(112, SlotSize),
			TooltipText = "End Turn",
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

	private static StyleBoxFlat MakeStyle(Color bg, Color border, int borderWidth, int radius)
	{
		return new StyleBoxFlat
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

	private static Texture2D LoadSvgIcon(string path, Color tint)
	{
		if (!Godot.FileAccess.FileExists(path))
			return ImageTexture.CreateFromImage(Image.CreateEmpty((int)IconPx, (int)IconPx, false, Image.Format.Rgba8));

		var svg = Godot.FileAccess.GetFileAsString(path);
		var image = new Image();
		var err = image.LoadSvgFromString(svg, IconPx / SvgSourceSize);
		if (err != Error.Ok)
		{
			GD.PushWarning($"ActionBar: failed to load icon '{path}' ({err})");
			return ImageTexture.CreateFromImage(Image.CreateEmpty((int)IconPx, (int)IconPx, false, Image.Format.Rgba8));
		}

		TintWhitePixels(image, tint);
		return ImageTexture.CreateFromImage(image);
	}

	private static void TintWhitePixels(Image image, Color tint)
	{
		var w = image.GetWidth();
		var h = image.GetHeight();
		for (var y = 0; y < h; y++)
		for (var x = 0; x < w; x++)
		{
			var p = image.GetPixel(x, y);
			if (p.A <= 0.001f)
				continue;
			image.SetPixel(x, y, new Color(tint.R, tint.G, tint.B, p.A * tint.A));
		}
	}
}
