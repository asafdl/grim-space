using Godot;

namespace GrimSpace.Battle.Presentation.Ui;

public sealed partial class ActionBar : HBoxContainer
{
	public event Action? FlakModeRequested;
	public event Action? RailgunModeRequested;
	public event Action? TorpedoModeRequested;
	public event Action? EndTurnRequested;

	private const int SlotSize = 64;
	private const float IconPx = 40f;
	private const float SvgSourceSize = 512f;

	private readonly ButtonGroup _modeGroup;
	private Button _flakButton = null!;
	private Button _railgunButton = null!;
	private Button _torpedoButton = null!;
	private PanelContainer _endTurnPanel = null!;
	private Button _endTurnButton = null!;
	private Label _flakCharges = null!;
	private Label _railgunCharges = null!;
	private Label _torpedoCharges = null!;

	public ActionBar(ButtonGroup modeGroup)
	{
		_modeGroup = modeGroup;
		MouseFilter = MouseFilterEnum.Ignore;
		Alignment = AlignmentMode.Center;
		AddThemeConstantOverride("separation", 14);
		Build();
	}

	public void SetMode(EPlayerMode mode)
	{
		_flakButton.SetBlockSignals(true);
		_railgunButton.SetBlockSignals(true);
		_torpedoButton.SetBlockSignals(true);

		_flakButton.ButtonPressed = mode == EPlayerMode.Flak;
		_railgunButton.ButtonPressed = mode == EPlayerMode.Railgun;
		_torpedoButton.ButtonPressed = mode == EPlayerMode.Torpedo;

		_flakButton.SetBlockSignals(false);
		_railgunButton.SetBlockSignals(false);
		_torpedoButton.SetBlockSignals(false);
	}

	public void Configure(
		bool canAct,
		bool isInspecting,
		bool showFlak,
		bool showRailgun,
		bool showTorpedo,
		bool flakEnabled,
		bool railgunEnabled,
		bool torpedoEnabled,
		string flakCharges,
		string railgunCharges,
		string torpedoCharges)
	{
		_flakButton.Visible = showFlak;
		_railgunButton.Visible = showRailgun;
		_torpedoButton.Visible = showTorpedo;
		_endTurnPanel.Visible = !isInspecting;

		_flakButton.Disabled = !canAct || !flakEnabled;
		_railgunButton.Disabled = !canAct || !railgunEnabled;
		_torpedoButton.Disabled = !canAct || !torpedoEnabled;
		_endTurnButton.Disabled = !canAct;
		_flakCharges.Text = flakCharges;
		_railgunCharges.Text = railgunCharges;
		_torpedoCharges.Text = torpedoCharges;
	}

	/// <summary>Activates ability slot 2–4 if enabled. Returns false if unbound or disabled.</summary>
	public bool TryActivateHotkey(int slot)
	{
		var button = slot switch
		{
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
		_flakButton = CreateAbilitySlot(
			hotkey: "2",
			tooltip: BattleHudCopy.FlakTooltip,
			iconPath: "res://assets/ui/abilities/flak.svg",
			accent: abilityAccent,
			onPressed: () => FlakModeRequested?.Invoke(),
			out _flakCharges);
		_railgunButton = CreateAbilitySlot(
			hotkey: "3",
			tooltip: BattleHudCopy.RailgunTooltip,
			iconPath: "res://assets/ui/abilities/railgun.svg",
			accent: abilityAccent,
			onPressed: () => RailgunModeRequested?.Invoke(),
			out _railgunCharges);
		_torpedoButton = CreateAbilitySlot(
			hotkey: "4",
			tooltip: BattleHudCopy.TorpedoTooltip,
			iconPath: "res://assets/ui/abilities/torpedo.svg",
			accent: abilityAccent,
			onPressed: () => TorpedoModeRequested?.Invoke(),
			out _torpedoCharges);

		abilityRow.AddChild(_flakButton);
		abilityRow.AddChild(_railgunButton);
		abilityRow.AddChild(_torpedoButton);

		_endTurnPanel = CreatePanel(new Color(0.16f, 0.1f, 0.05f, 0.94f), new Color(0.95f, 0.7f, 0.25f, 0.9f));
		AddChild(_endTurnPanel);

		var endPad = CreatePad();
		_endTurnPanel.AddChild(endPad);

		_endTurnButton = CreateEndTurnButton();
		endPad.AddChild(_endTurnButton);
	}

	private Button CreateAbilitySlot(
		string hotkey,
		string tooltip,
		string iconPath,
		Color accent,
		Action onPressed,
		out Label charges)
	{
		var button = new Button
		{
			ToggleMode = true,
			ButtonGroup = _modeGroup,
			CustomMinimumSize = new Vector2(SlotSize, SlotSize),
			Icon = LoadSvgIcon(iconPath, accent),
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
				onPressed();
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
