using Godot;
using GrimSpace.Components;
using GrimSpace.Pilots;
using GrimSpace.Run;
using GrimSpace.Units;

namespace GrimSpace.World.StarSystem.Presentation.Ui;

public sealed partial class PilotCharacterSheetOverlay : Control
{
	private const string ShieldIconPath = "res://assets/ui/abilities/shields_up.svg";
	private const string LightningIconPath = "res://assets/ui/abilities/lightning_cannon.svg";
	private const string SalvageIconPath = "res://assets/ui/abilities/scrap_drone_swarm.svg";
	private const string BotIconPath = "res://assets/ui/abilities/repurposed-miner.svg";
	private const string DodgeIconPath = "res://assets/ui/abilities/spin.svg";
	private const string ManeuverIconPath = "res://assets/ui/abilities/yaw.svg";
	private const string FrayIconPath = "res://assets/ui/abilities/focus.svg";
	private const string BackgroundIconPath = "res://assets/ui/pilots/background-placeholder.svg";
	private const string PositiveQuirkIconPath = "res://assets/ui/pilots/positive-quirk-placeholder.svg";
	private const string NegativeQuirkIconPath = "res://assets/ui/pilots/negative-quirk-placeholder.svg";
	private const string PilotCursorPath = "res://assets/ui/turn_flow/active_cursor.svg";
	private const int SkillNodeSize = 60;
	private const int IdentityIconSize = 48;
	private const int PortraitSize = 144;
	private const int IdentityRailWidth = 204;

	private static readonly Color AvailableColor = HudPalettes.PurpleRed.Primary;
	private static readonly Color LearnedColor = new(0.42f, 0.88f, 0.58f);
	private static readonly Color LockedColor = new(0.32f, 0.38f, 0.46f);
	private static readonly Color BackgroundColor = HudPalettes.BlueCyan.Primary;
	private static readonly Color PositiveQuirkColor = HudPalettes.Gold.Primary;
	private static readonly Color NegativeQuirkColor = HudStyles.TextColor(HudTextRole.Danger);

	private readonly ModalShell _shell;
	private string? _pilotId;

	public event Action<string, EPilotSkillId>? SkillLearnRequested;
	public event Action<string, int>? PilotCycleRequested;
	public event Action<string>? FleetManagementRequested;
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
		_shell.SetDismissVisible(true);
		_shell.Closed += () => Closed?.Invoke();
		AddChild(_shell);
	}

	public bool IsOpen => _shell.IsOpen;

	public void Close() => _shell.Close();

	public bool TryHandleInput(InputEvent @event) => _shell.TryHandleInput(@event);

	public void Open(Pilot pilot, bool canCyclePilots, ShipInstance? assignedShip = null)
	{
		ArgumentNullException.ThrowIfNull(pilot);
		_pilotId = pilot.Id;
		_shell.Open("");
		_shell.SetHeader(
			HudHeaderMode.Back,
			() => FleetManagementRequested?.Invoke(pilot.Id),
			"Fleet Management");
		_shell.SetBackHandler(null);
		_shell.SetFooter([]);
		_shell.SetBody(BuildBody(pilot, canCyclePilots, assignedShip));
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
		portraitRow.AddChild(new TextureRect
		{
			Texture = ShipPilotPortraitCatalog.TextureFor(pilot.PortraitId),
			CustomMinimumSize = new Vector2(PortraitSize, PortraitSize),
			ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
			StretchMode = TextureRect.StretchModeEnum.KeepAspectCovered,
			SizeFlagsHorizontal = SizeFlags.ShrinkCenter,
			MouseFilter = MouseFilterEnum.Ignore,
		});
		portraitRow.AddChild(BuildPilotCursor(1, canCyclePilots));
		rail.AddChild(portraitRow);

		var name = new Label
		{
			Text = pilot.Name,
			HorizontalAlignment = HorizontalAlignment.Center,
			AutowrapMode = TextServer.AutowrapMode.WordSmart,
			MouseFilter = MouseFilterEnum.Ignore,
		};
		HudStyles.ApplyTextRole(name, HudTextRole.Emphasis);
		rail.AddChild(name);

		var level = new Label
		{
			Text = $"LEVEL {pilot.ClaimedLevel}",
			HorizontalAlignment = HorizontalAlignment.Center,
			MouseFilter = MouseFilterEnum.Ignore,
		};
		HudStyles.ApplyTextRole(level, HudTextRole.Metadata);
		rail.AddChild(level);

		var background = PilotBackgroundCatalog.Get(pilot.BackgroundId);
		var positiveQuirk = PilotQuirkCatalog.Get(pilot.PositiveQuirkId);
		var negativeQuirk = PilotQuirkCatalog.Get(pilot.NegativeQuirkId);
		var identityIcons = new HBoxContainer
		{
			Alignment = BoxContainer.AlignmentMode.Center,
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
		};
		identityIcons.AddThemeConstantOverride("separation", 10);
		identityIcons.AddChild(BuildIdentityIcon(
			BackgroundIconPathFor(background.Id),
			BackgroundColor,
			$"Background: {background.Name}\n{background.Description}"));
		identityIcons.AddChild(BuildIdentityIcon(
			QuirkIconPathFor(positiveQuirk.Id),
			PositiveQuirkColor,
			$"Positive Quirk: {positiveQuirk.Name}\n{positiveQuirk.Description}"));
		identityIcons.AddChild(BuildIdentityIcon(
			QuirkIconPathFor(negativeQuirk.Id),
			NegativeQuirkColor,
			$"Negative Quirk: {negativeQuirk.Name}\n{negativeQuirk.Description}"));
		rail.AddChild(identityIcons);

		if (assignedShip is not null)
		{
			var spacer = new Control { CustomMinimumSize = new Vector2(0, 8) };
			rail.AddChild(spacer);
			rail.AddChild(BuildMeter(
				"SHIELD",
				assignedShip.TotalCurrentShieldPoints,
				assignedShip.TotalMaxShieldPoints,
				new Color(0.25f, 0.62f, 0.95f)));
		}

		var (currentXp, requiredXp) = PilotProgression.ProgressWithinLevel(pilot.ExperiencePoints);
		rail.AddChild(BuildMeter(
			"XP",
			requiredXp > 0 ? currentXp : 1,
			requiredXp > 0 ? requiredXp : 1,
			HudPalettes.PurpleRed.Secondary,
			requiredXp > 0 ? $"{currentXp} / {requiredXp}" : "MAX"));
		return rail;
	}

	private static Button BuildIdentityIcon(string iconPath, Color color, string tooltip)
	{
		var button = new Button
		{
			CustomMinimumSize = new Vector2(IdentityIconSize, IdentityIconSize),
			Icon = SvgIconLoader.Load(iconPath, color, 28),
			ExpandIcon = false,
			IconAlignment = HorizontalAlignment.Center,
			VerticalIconAlignment = VerticalAlignment.Center,
			FocusMode = FocusModeEnum.None,
			MouseDefaultCursorShape = CursorShape.Help,
			TooltipText = tooltip,
		};
		button.AddThemeStyleboxOverride("normal", IdentityIconStyle(color, 0.1f, 2));
		button.AddThemeStyleboxOverride("hover", IdentityIconStyle(color, 0.2f, 3));
		button.AddThemeStyleboxOverride("pressed", IdentityIconStyle(color, 0.1f, 2));
		return button;
	}

	private static StyleBoxFlat IdentityIconStyle(Color color, float backgroundAlpha, int borderWidth) =>
		new()
		{
			BgColor = new Color(color.R, color.G, color.B, backgroundAlpha),
			BorderColor = color,
			BorderWidthLeft = borderWidth,
			BorderWidthTop = borderWidth,
			BorderWidthRight = borderWidth,
			BorderWidthBottom = borderWidth,
			CornerRadiusTopLeft = 8,
			CornerRadiusTopRight = 8,
			CornerRadiusBottomLeft = 8,
			CornerRadiusBottomRight = 8,
		};

	private Control BuildPilotCursor(int direction, bool visible)
	{
		var host = new CenterContainer
		{
			CustomMinimumSize = new Vector2(20, PortraitSize),
			Visible = visible,
			MouseFilter = MouseFilterEnum.Stop,
			MouseDefaultCursorShape = CursorShape.PointingHand,
			TooltipText = direction < 0 ? "Previous pilot" : "Next pilot",
		};
		host.AddChild(new TextureRect
		{
			Texture = ResourceLoader.Load<Texture2D>(PilotCursorPath),
			CustomMinimumSize = new Vector2(20, 16),
			ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
			StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
			RotationDegrees = direction < 0 ? -90f : 90f,
			PivotOffset = new Vector2(10, 8),
			MouseFilter = MouseFilterEnum.Ignore,
		});
		host.GuiInput += @event =>
		{
			if (@event is InputEventMouseButton
				{
					Pressed: true,
					ButtonIndex: MouseButton.Left,
				}
				&& _pilotId is not null)
				PilotCycleRequested?.Invoke(_pilotId, direction);
		};
		return host;
	}

	private static Control BuildMeter(
		string title,
		int current,
		int max,
		Color color,
		string? valueText = null)
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
		meter.AddChild(caption);

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
		for (var level = 2; level <= PilotProgression.MaxLevel; level++)
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
		row.AddChild(levelLabel);

		var definitions = PilotSkillCatalog.All
			.Where(definition => definition.Tier + 1 == level)
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

		var button = new Button
		{
			CustomMinimumSize = new Vector2(SkillNodeSize, SkillNodeSize),
			Disabled = !canLearn,
			Icon = SvgIconLoader.Load(IconPathFor(definition.Id), color, 34),
			ExpandIcon = false,
			IconAlignment = HorizontalAlignment.Center,
			VerticalIconAlignment = VerticalAlignment.Center,
			TooltipText = $"{definition.Name}\n{definition.Description}",
		};
		button.AddThemeColorOverride("icon_disabled_color", Colors.White);
		ApplyCircleStyle(button, color, isLearned);
		if (canLearn)
			button.Pressed += () => SkillLearnRequested?.Invoke(pilot.Id, definition.Id);
		return button;
	}

	private static string IconPathFor(EPilotSkillId skillId) =>
		skillId switch
		{
			EPilotSkillId.ShieldsUp => ShieldIconPath,
			EPilotSkillId.DodgeThis => DodgeIconPath,
			EPilotSkillId.IonizedPayload => LightningIconPath,
			EPilotSkillId.DefensiveManeuvers => ManeuverIconPath,
			EPilotSkillId.InTheFray => FrayIconPath,
			EPilotSkillId.DronesYearnForMines => SalvageIconPath,
			EPilotSkillId.IKnowABot => BotIconPath,
			_ => throw new ArgumentOutOfRangeException(nameof(skillId), skillId, null),
		};

	private static string BackgroundIconPathFor(EPilotBackgroundId backgroundId) =>
		backgroundId switch
		{
			EPilotBackgroundId.Unspecified => BackgroundIconPath,
			_ => throw new ArgumentOutOfRangeException(nameof(backgroundId), backgroundId, null),
		};

	private static string QuirkIconPathFor(EPilotQuirkId quirkId) =>
		quirkId switch
		{
			EPilotQuirkId.PlaceholderPositive or EPilotQuirkId.GrantProbePositive =>
				PositiveQuirkIconPath,
			EPilotQuirkId.PlaceholderNegative or EPilotQuirkId.GrantProbeNegative =>
				NegativeQuirkIconPath,
			_ => throw new ArgumentOutOfRangeException(nameof(quirkId), quirkId, null),
		};

	private static void ApplyCircleStyle(Button button, Color color, bool learned)
	{
		button.AddThemeStyleboxOverride("normal", CircleStyle(color, learned ? 0.28f : 0.12f, 2));
		button.AddThemeStyleboxOverride("hover", CircleStyle(color, 0.22f, 3));
		button.AddThemeStyleboxOverride("pressed", CircleStyle(color, 0.34f, 3));
		button.AddThemeStyleboxOverride("disabled", CircleStyle(color, learned ? 0.24f : 0.08f, 2));
	}

	private static StyleBoxFlat CircleStyle(Color color, float backgroundAlpha, int borderWidth) =>
		new()
		{
			BgColor = new Color(color.R, color.G, color.B, backgroundAlpha),
			BorderColor = color,
			BorderWidthLeft = borderWidth,
			BorderWidthTop = borderWidth,
			BorderWidthRight = borderWidth,
			BorderWidthBottom = borderWidth,
			CornerRadiusTopLeft = SkillNodeSize / 2,
			CornerRadiusTopRight = SkillNodeSize / 2,
			CornerRadiusBottomLeft = SkillNodeSize / 2,
			CornerRadiusBottomRight = SkillNodeSize / 2,
		};
}
