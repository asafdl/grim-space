using Godot;
using System.Text;
using GrimSpace.Components;
using GrimSpace.Pilots;
using GrimSpace.Run;
using GrimSpace.Ui.Components;
using GrimSpace.Ui.Portraits;
using GrimSpace.Units;

namespace GrimSpace.World.StarSystem.Presentation.Ui;

public sealed partial class PilotCharacterSheetOverlay : Control
{
	private const string ShieldIconPath = "res://assets/ui/abilities/shields_up.svg";
	private const string LightningIconPath = "res://assets/ui/abilities/lightning_cannon.svg";
	private const string SalvageIconPath = "res://assets/ui/abilities/scrap_drone_swarm.svg";
	private const string BotIconPath = "res://assets/ui/abilities/repurposed-miner.svg";
	private const string DodgeIconPath = "res://assets/ui/abilities/spin.svg";
	private const string OverchargeEnginesIconPath = "res://assets/ui/abilities/yaw.svg";
	private const string ManeuverIconPath = "res://assets/ui/abilities/yaw.svg";
	private const string FrayIconPath = "res://assets/ui/abilities/focus.svg";
	private const string VoidBombIconPath = "res://assets/ui/abilities/void_bomb.svg";
	private const string HullIconPath = "res://assets/ui/battle/hull.png";
	private const string HeatIconPath = "res://assets/ui/battle/heat.png";
	private const string BetterRamIconPath = "res://assets/ui/abilities/better_ram.png";
	private const string ReactiveShieldsIconPath = "res://assets/ui/abilities/reactive_shields.png";
	private const string HealIconPath = "res://assets/ui/battle/heal.png";
	private const string CritIconPath = "res://assets/ui/battle/crit.png";
	private const string PaletteIconPath = "res://assets/ui/pilots/randomize-palette.png";
	private const string SkillReticlePath =
		"res://assets/ui/components/runtime/reticle_hollow_neutral.png";
	private const int SkillNodeSize = 60;
	private const int SkillIconSize = 26;
	private const int PortraitSize = 144;
	private const int IdentityRailWidth = 204;
	private const int IdentityMaxLength = 15;

	private static readonly Color AvailableColor = HudPalettes.PurpleRed.Primary;
	private static readonly Color LearnedColor = new(0.42f, 0.88f, 0.58f);
	private static readonly Color LockedColor = new(0.32f, 0.38f, 0.46f);
	private static readonly Color SkillReticleColor =
		HudPalettes.WithAlpha(HudPalettes.BlueCyan.Highlight, 0.82f);
	private static readonly Texture2D SkillReticle = GD.Load<Texture2D>(SkillReticlePath);
	private readonly ModalShell _shell;
	private readonly CompactConfirmationPopup _dismissPopup;
	private string? _pilotId;
	private Control? _identityEditArea;
	private Action? _cancelIdentityEdit;

	public event Action<string, EPilotSkillId>? SkillLearnRequested;
	public event Action<string, int>? PilotCycleRequested;
	public event Action<string>? DismissRequested;
	public event Action<string, string, string>? IdentityEdited;
	public event Action<string>? PaletteRandomizeRequested;
	public event Action? Closed;

	public PilotCharacterSheetOverlay()
	{
		AnchorsPreset = (int)LayoutPreset.FullRect;
		AnchorRight = 1f;
		AnchorBottom = 1f;
		GrowHorizontal = GrowDirection.Both;
		GrowVertical = GrowDirection.Both;
		MouseFilter = MouseFilterEnum.Ignore;

		_shell = new ModalShell(HudThemeFamily.Informative);
		var sheetTheme = (Theme)HudThemes.Load(HudThemeFamily.Informative).Duplicate();
		sheetTheme.SetColor("font_color", "Label", HudPalettes.BlueCyan.Primary);
		sheetTheme.SetColor("font_color", "TextEdit", HudPalettes.BlueCyan.Title);
		sheetTheme.SetColor("caret_color", "TextEdit", HudPalettes.BlueCyan.Title);
		_shell.Theme = sheetTheme;
		_shell.SetDismissVisible(true);
		_shell.Closed += () =>
		{
			_cancelIdentityEdit = null;
			_identityEditArea = null;
			_dismissPopup.Hide();
			Closed?.Invoke();
		};
		AddChild(_shell);
		_dismissPopup = new CompactConfirmationPopup();
		AddChild(_dismissPopup);
	}

	public bool IsOpen => _shell.IsOpen;

	public void Close()
	{
		_cancelIdentityEdit = null;
		_identityEditArea = null;
		_dismissPopup.Hide();
		_shell.Close();
	}

	public bool TryHandleInput(InputEvent @event) => _shell.TryHandleInput(@event);

	public override void _Input(InputEvent @event)
	{
		if (_shell.IsOpen
			&& _cancelIdentityEdit is not null
			&& _identityEditArea is not null
			&& @event is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left }
			&& !_identityEditArea.GetGlobalRect().HasPoint(_identityEditArea.GetGlobalMousePosition()))
			_cancelIdentityEdit();
	}

	public void Open(
		Pilot pilot,
		bool canCyclePilots,
		ShipInstance? assignedShip = null,
		bool canDismiss = true)
	{
		ArgumentNullException.ThrowIfNull(pilot);
		_pilotId = pilot.Id;
		_cancelIdentityEdit = null;
		_identityEditArea = null;
		_dismissPopup.Hide();
		_shell.Open("Character Sheet");
		_shell.SetHeader(HudHeaderMode.Close);
		_shell.SetBackHandler(null);
		_shell.SetFooter(
		[
			new HudAction(
				"Dismiss",
				HudActionKind.Destructive,
				ShowDismissConfirm,
				enabled: canDismiss),
		]);
		_shell.SetBody(BuildBody(pilot, canCyclePilots, assignedShip));
	}

	private void ShowDismissConfirm(BaseButton anchor)
	{
		if (_pilotId is null)
			return;

		var pilotId = _pilotId;
		_dismissPopup.Request(anchor, () => DismissRequested?.Invoke(pilotId));
	}

	private Control BuildBody(Pilot pilot, bool canCyclePilots, ShipInstance? assignedShip)
	{
		var body = new HBoxContainer
		{
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
			SizeFlagsVertical = SizeFlags.ExpandFill,
		};
		body.AddThemeConstantOverride("separation", 18);
		body.AddChild(BuildIdentityRail(pilot, canCyclePilots, assignedShip));
		body.AddChild(new VSeparator());
		var skills = BuildTree(pilot);
		skills.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		body.AddChild(skills);
		return body;
	}

	private Control BuildIdentityRail(Pilot pilot, bool canCyclePilots, ShipInstance? assignedShip)
	{
		var rail = new VBoxContainer
		{
			CustomMinimumSize = new Vector2(IdentityRailWidth, 0),
			SizeFlagsHorizontal = SizeFlags.ShrinkBegin,
		};
		rail.AddThemeConstantOverride("separation", 8);

		var portraitRow = new HBoxContainer
		{
			Alignment = BoxContainer.AlignmentMode.Center,
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
		};
		portraitRow.AddThemeConstantOverride("separation", 6);
		portraitRow.AddChild(BuildPilotCursor(-1, canCyclePilots));
		var portrait = new UiFramedPortrait(
			new PilotPortrait(pilot.PortraitId))
		{
			SizeOverride = new UiSizeOverride(PortraitSize, PortraitSize),
			SizeFlagsHorizontal = SizeFlags.ShrinkCenter,
		};
		var randomizePalette = new TextureButton
		{
			Position = Vector2.Zero,
			Size = new Vector2(28, 28),
			TextureNormal = GD.Load<Texture2D>(PaletteIconPath)
				?? throw new InvalidOperationException($"Could not load palette icon '{PaletteIconPath}'."),
			IgnoreTextureSize = true,
			StretchMode = TextureButton.StretchModeEnum.KeepAspectCentered,
			Modulate = HudPalettes.BlueCyan.Title,
			MouseDefaultCursorShape = CursorShape.PointingHand,
		};
		randomizePalette.Pressed += () =>
		{
			if (_pilotId is not null)
				PaletteRandomizeRequested?.Invoke(_pilotId);
		};
		portrait.AddChild(randomizePalette);
		portraitRow.AddChild(portrait);
		portraitRow.AddChild(BuildPilotCursor(1, canCyclePilots));
		rail.AddChild(portraitRow);
		rail.AddChild(BuildIdentityEditor(pilot));

		var identityIcons = PilotIdentityIconRail.BuildFor(pilot);
		rail.AddChild(identityIcons);

		if (assignedShip is not null)
		{
			var spacer = new Control { CustomMinimumSize = new Vector2(0, 8) };
			rail.AddChild(spacer);
			rail.AddChild(BuildMeter(
				"SHIELD",
				assignedShip.TotalCurrentShieldPoints,
				assignedShip.TotalBaseShieldPoints,
				new Color(0.25f, 0.62f, 0.95f)));
			rail.AddChild(BuildMeter(
				"HULL",
				assignedShip.CurrentHullPoints,
				assignedShip.Loadout.BaseHullPoints,
				new Color(0.85f, 0.15f, 0.18f)));
		}

		var (currentXp, requiredXp) = PilotProgression.ProgressWithinLevel(pilot.ExperiencePoints);
		rail.AddChild(BuildMeter(
			"XP",
			requiredXp > 0 ? currentXp : 1,
			requiredXp > 0 ? requiredXp : 1,
			HudPalettes.PurpleRed.Secondary,
			requiredXp > 0 ? $"{currentXp} / {requiredXp}" : "MAX",
			rightCaption: $"LEVEL {pilot.ClaimedLevel}"));
		return rail;
	}

	private Control BuildIdentityEditor(Pilot pilot)
	{
		var host = new Control
		{
			CustomMinimumSize = new Vector2(IdentityRailWidth, 64),
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
		};
		var display = new VBoxContainer
		{
			AnchorRight = 1f,
			AnchorBottom = 1f,
			MouseFilter = MouseFilterEnum.Ignore,
		};
		display.AddThemeConstantOverride("separation", 2);
		foreach (var text in new[] { pilot.TaggedName, $"\"{pilot.Nickname}\"" })
		{
			var label = new Label
			{
				Text = text,
				AutowrapMode = TextServer.AutowrapMode.WordSmart,
				HorizontalAlignment = HorizontalAlignment.Center,
				MouseFilter = MouseFilterEnum.Ignore,
			};
			HudStyles.ApplyTextRole(label, HudTextRole.Metadata);
			label.AddThemeColorOverride("font_color", HudPalettes.BlueCyan.Secondary);
			display.AddChild(label);
		}
		host.AddChild(display);

		var displayButton = new Button
		{
			Flat = true,
			TooltipText = "Edit",
			MouseDefaultCursorShape = CursorShape.PointingHand,
			AnchorRight = 1f,
			AnchorBottom = 1f,
		};
		host.AddChild(displayButton);

		var fields = new VBoxContainer
		{
			Visible = false,
			AnchorRight = 1f,
			AnchorBottom = 1f,
		};
		fields.AddThemeConstantOverride("separation", 2);
		var name = BuildIdentityField(pilot.Name, "First name");
		var nickname = BuildIdentityField(pilot.Nickname, "Nickname");
		fields.AddChild(name);
		fields.AddChild(nickname);
		host.AddChild(fields);

		void UpdateHeight()
		{
			var height = Mathf.Max(64, Mathf.Max(
				display.GetCombinedMinimumSize().Y,
				fields.GetCombinedMinimumSize().Y));
			host.CustomMinimumSize = new Vector2(IdentityRailWidth, height);
		}

		void Cancel()
		{
			_cancelIdentityEdit = null;
			_identityEditArea = null;
			name.Text = LimitIdentity(pilot.Name);
			nickname.Text = LimitIdentity(pilot.Nickname);
			name.ReleaseFocus();
			nickname.ReleaseFocus();
			fields.Visible = false;
			display.Visible = true;
			displayButton.Visible = true;
		}

		void Save()
		{
			var nextName = string.IsNullOrWhiteSpace(name.Text) ? pilot.Name : name.Text.Trim();
			var nextNickname = string.IsNullOrWhiteSpace(nickname.Text) ? pilot.Nickname : nickname.Text.Trim();
			Cancel();
			if (nextName != pilot.Name || nextNickname != pilot.Nickname)
				IdentityEdited?.Invoke(pilot.Id, nextName, nextNickname);
		}

		void HandleInput(InputEvent input, TextEdit field)
		{
			if (_cancelIdentityEdit is null
				|| input is not InputEventKey { Pressed: true, Echo: false } key)
				return;
			if (key.Keycode is Key.Enter or Key.KpEnter)
			{
				field.AcceptEvent();
				Save();
			}
			else if (key.Keycode == Key.Escape)
			{
				field.AcceptEvent();
				Cancel();
			}
		}

		displayButton.Pressed += () =>
		{
			display.Visible = false;
			displayButton.Visible = false;
			fields.Visible = true;
			_identityEditArea = host;
			_cancelIdentityEdit = Cancel;
			name.GrabFocus();
		};
		name.GuiInput += input => HandleInput(input, name);
		nickname.GuiInput += input => HandleInput(input, nickname);
		display.MinimumSizeChanged += UpdateHeight;
		fields.MinimumSizeChanged += UpdateHeight;
		host.Ready += UpdateHeight;
		return host;
	}

	private static TextEdit BuildIdentityField(string value, string label)
	{
		var field = new TextEdit
		{
			Text = LimitIdentity(value),
			TooltipText = $"{label}: Enter to save, Escape to cancel",
			CustomMinimumSize = new Vector2(40, 0),
			SizeFlagsHorizontal = SizeFlags.ShrinkCenter,
			ScrollFitContentHeight = true,
			WrapMode = TextEdit.LineWrappingMode.Boundary,
			TabInputMode = false,
			CaretMultiple = false,
		};
		field.AddThemeStyleboxOverride("normal", new StyleBoxFlat
		{
			BgColor = HudPalettes.BlueCyan.Surface.Lerp(HudPalettes.BlueCyan.Border, 0.2f),
		});
		field.AddThemeStyleboxOverride("focus", new StyleBoxEmpty());
		void UpdateWidth()
		{
			if (!field.IsInsideTree())
				return;

			var font = field.GetThemeFont("font");
			var fontSize = field.GetThemeFontSize("font_size");
			var width = font.GetStringSize(field.Text, fontSize: fontSize).X;
			field.CustomMinimumSize = new Vector2(
				Mathf.Clamp(Mathf.Ceil(width) + 12, 40, IdentityRailWidth),
				0);
		}

		var lastValid = field.Text;
		field.Ready += UpdateWidth;
		field.TextChanged += () =>
		{
			if (field.Text.EnumerateRunes().Count() > IdentityMaxLength
				|| field.Text.EnumerateRunes().Any(rune =>
					!Rune.IsLetterOrDigit(rune) && rune.Value is not (' ' or '-')))
			{
				var line = field.GetCaretLine();
				var column = field.GetCaretColumn();
				field.Text = lastValid;
				line = Mathf.Min(line, field.GetLineCount() - 1);
				field.SetCaretLine(line);
				field.SetCaretColumn(Mathf.Min(column, field.GetLine(line).EnumerateRunes().Count()));
			}
			else
			{
				lastValid = field.Text;
			}
			UpdateWidth();
		};
		return field;
	}

	private static string LimitIdentity(string value) =>
		string.Concat(value.EnumerateRunes().Take(IdentityMaxLength).Select(rune => rune.ToString()));

	private Control BuildPilotCursor(int direction, bool visible)
	{
		var host = HudCycleArrow.Create(direction, PortraitSize, direction < 0 ? "Previous pilot" : "Next pilot", () =>
		{
			if (_pilotId is not null)
				PilotCycleRequested?.Invoke(_pilotId, direction);
		});
		host.Visible = visible;
		return host;
	}

	private static Control BuildMeter(
		string title,
		int current,
		int max,
		Color color,
		string? valueText = null,
		string? rightCaption = null)
	{
		max = Mathf.Max(1, max);
		current = Mathf.Clamp(current, 0, max);
		var meter = new VBoxContainer();
		meter.AddThemeConstantOverride("separation", 3);

		var caption = new Label
		{
			Text = $"{title}  {valueText ?? $"{current} / {max}"}",
			MouseFilter = MouseFilterEnum.Ignore,
		};
		HudStyles.ApplyTextRole(caption, HudTextRole.Metadata);
		caption.AddThemeColorOverride("font_color", HudPalettes.BlueCyan.Primary);
		if (rightCaption is null)
		{
			meter.AddChild(caption);
		}
		else
		{
			var heading = new HBoxContainer();
			caption.SizeFlagsHorizontal = SizeFlags.ExpandFill;
			heading.AddChild(caption);
			var level = new Label { Text = rightCaption, MouseFilter = MouseFilterEnum.Ignore };
			HudStyles.ApplyTextRole(level, HudTextRole.Metadata);
			level.AddThemeColorOverride("font_color", HudPalettes.BlueCyan.Primary);
			heading.AddChild(level);
			meter.AddChild(heading);
		}

		var bar = new ProgressBar
		{
			CustomMinimumSize = new Vector2(0, 7),
			MaxValue = max,
			Value = current,
			ShowPercentage = false,
			MouseFilter = MouseFilterEnum.Ignore,
		};
		bar.AddThemeStyleboxOverride("fill", BarStyle(color));
		bar.AddThemeStyleboxOverride(
			"background",
			BarStyle(HudPalettes.WithAlpha(HudPalettes.BlueCyan.Surface, 0.95f)));
		meter.AddChild(bar);
		return meter;
	}

	private static StyleBoxFlat BarStyle(Color color) =>
		new()
		{
			BgColor = color,
			CornerRadiusTopLeft = 2,
			CornerRadiusTopRight = 2,
			CornerRadiusBottomLeft = 2,
			CornerRadiusBottomRight = 2,
		};

	private Control BuildTree(Pilot pilot)
	{
		var body = new VBoxContainer
		{
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
		};
		body.AddThemeConstantOverride("separation", 8);

		var available = PilotSkillAvailability.AvailableToLearn(pilot).ToHashSet();
		var learned = pilot.LearnedSkills.ToHashSet();
		for (var level = 1; level <= PilotProgression.MaxLevel; level++)
			body.AddChild(BuildLevelRow(pilot, level, learned, available));

		return body;
	}

	private Control BuildLevelRow(
		Pilot pilot,
		int level,
		HashSet<EPilotSkillId> learned,
		HashSet<EPilotSkillId> available)
	{
		var row = new HBoxContainer
		{
			CustomMinimumSize = new Vector2(0, 68),
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
		};
		row.AddThemeConstantOverride("separation", 10);

		var levelLabel = new Label
		{
			Text = $"LEVEL {level}",
			CustomMinimumSize = new Vector2(72, 0),
			VerticalAlignment = VerticalAlignment.Center,
		};
		HudStyles.ApplyTextRole(levelLabel, HudTextRole.Metadata);
		levelLabel.AddThemeColorOverride("font_color", HudPalettes.BlueCyan.Primary);
		row.AddChild(levelLabel);

		var definitions = PilotSkillAvailability.TreeFor(pilot)
			.Where(definition => definition.MinimumEarnedLevel == level)
			.ToArray();
		if (definitions.Length == 0)
		{
			row.AddChild(CreateLevelLine(expand: true));
			return row;
		}

		row.AddChild(CreateLevelLine(expand: false));
		foreach (var definition in definitions)
		{
			row.AddChild(BuildSkillNode(pilot, definition, learned, available));
			row.AddChild(CreateLevelLine(expand: true));
		}

		return row;
	}

	private static ColorRect CreateLevelLine(bool expand) =>
		new()
		{
			Color = HudPalettes.WithAlpha(HudPalettes.BlueCyan.Border, 0.7f),
			CustomMinimumSize = new Vector2(expand ? 0 : 28, 2),
			SizeFlagsHorizontal = expand ? SizeFlags.ExpandFill : SizeFlags.ShrinkBegin,
			SizeFlagsVertical = SizeFlags.ShrinkCenter,
			MouseFilter = MouseFilterEnum.Ignore,
		};

	private Control BuildSkillNode(
		Pilot pilot,
		PilotSkillDefinition definition,
		HashSet<EPilotSkillId> learned,
		HashSet<EPilotSkillId> available)
	{
		var isLearned = learned.Contains(definition.Id);
		var canLearn = available.Contains(definition.Id);
		var color = isLearned ? LearnedColor : canLearn ? AvailableColor : LockedColor;

		var host = new HudTooltipHost
		{
			CustomMinimumSize = new Vector2(SkillNodeSize, SkillNodeSize),
			MouseFilter = MouseFilterEnum.Stop,
		};
		host.SetTooltip(definition.SheetTooltip);

		var frameColor = !isLearned && !canLearn
			? HudPalettes.WithAlpha(HudPalettes.BlueCyan.Highlight, 0.35f)
			: SkillReticleColor;

		var button = new Button
		{
			CustomMinimumSize = new Vector2(SkillNodeSize, SkillNodeSize),
			Disabled = !canLearn,
			Icon = SvgIconLoader.LoadCentered(IconPathFor(definition.Id), color, SkillIconSize),
			ExpandIcon = false,
			IconAlignment = HorizontalAlignment.Center,
			VerticalIconAlignment = VerticalAlignment.Center,
			FocusMode = FocusModeEnum.None,
			MouseFilter = MouseFilterEnum.Ignore,
		};
		button.AddThemeConstantOverride("h_separation", 0);
		button.AddThemeColorOverride("icon_disabled_color", Colors.White);
		ApplySkillReticleBorder(button, frameColor);

		if (canLearn)
		{
			host.MouseDefaultCursorShape = CursorShape.PointingHand;
			host.GuiInput += @event =>
			{
				if (@event is InputEventMouseButton
					{
						Pressed: true,
						ButtonIndex: MouseButton.Left,
					})
					SkillLearnRequested?.Invoke(pilot.Id, definition.Id);
			};
		}
		host.AddChild(button);
		return host;
	}

	private static void ApplySkillReticleBorder(Button button, Color frameColor)
	{
		button.AddThemeStyleboxOverride("normal", SkillReticleBorderStyle(frameColor));
		button.AddThemeStyleboxOverride(
			"hover",
			SkillReticleBorderStyle(frameColor.Lightened(0.12f)));
		button.AddThemeStyleboxOverride("pressed", SkillReticleBorderStyle(frameColor));
		button.AddThemeStyleboxOverride("disabled", SkillReticleBorderStyle(frameColor));
		button.AddThemeStyleboxOverride("focus", new StyleBoxEmpty());
	}

	private static StyleBoxTexture SkillReticleBorderStyle(Color modulate) =>
		new()
		{
			Texture = SkillReticle,
			ModulateColor = modulate,
		};

	private static string IconPathFor(EPilotSkillId skillId) =>
		skillId switch
		{
			EPilotSkillId.ShieldsUp => ShieldIconPath,
			EPilotSkillId.OverchargeEngines => OverchargeEnginesIconPath,
			EPilotSkillId.DodgeThis => DodgeIconPath,
			EPilotSkillId.IonizedPayload => LightningIconPath,
			EPilotSkillId.MarkWeakspot => CritIconPath,
			EPilotSkillId.ThirstForScrap => CritIconPath,
			EPilotSkillId.YouLittleRat => DodgeIconPath,
			EPilotSkillId.NothingButMetal => HullIconPath,
			EPilotSkillId.NotFullyFormed => SalvageIconPath,
			EPilotSkillId.ShieldDynamo => ShieldIconPath,
			EPilotSkillId.DefensiveManeuvers => ManeuverIconPath,
			EPilotSkillId.Brace => ShieldIconPath,
			EPilotSkillId.InTheFray => FrayIconPath,
			EPilotSkillId.OneWithTheVoid => VoidBombIconPath,
			EPilotSkillId.DronesYearnForMines => SalvageIconPath,
			EPilotSkillId.ReinforcedHull => HullIconPath,
			EPilotSkillId.BetterRam => BetterRamIconPath,
			EPilotSkillId.HeatVents => HeatIconPath,
			EPilotSkillId.ReactiveShields => ReactiveShieldsIconPath,
			EPilotSkillId.HelpABotterOut => HealIconPath,
			EPilotSkillId.IKnowABot => BotIconPath,
			_ => throw new ArgumentOutOfRangeException(nameof(skillId), skillId, null),
		};
}
