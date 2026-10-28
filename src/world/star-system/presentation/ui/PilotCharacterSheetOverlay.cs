using Godot;
using GrimSpace.Components;
using GrimSpace.Pilots;
using GrimSpace.Run;
using GrimSpace.Ui.Components;
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
	private const string ScrapDroneBackgroundIconPath = "res://assets/ui/pilots/scrap-drone.png";
	private const string SpaceRangerClassIconPath = "res://assets/ui/classes/space-ranger.png";
	private const string SupportClassIconPath = "res://assets/ui/classes/support.png";
	private const string IconBorderPath = "res://assets/ui/components/runtime/icon_border_blue.png";
	private const string OptimalityEnforcerIconPath = "res://assets/ui/pilots/optimality-enforcer.png";
	private const string CorporateSlaveIconPath = "res://assets/ui/pilots/corporate-slave.png";
	private const string PodRacerIconPath = "res://assets/ui/pilots/pod-racer.png";
	private const string AsteroidSurferIconPath = "res://assets/ui/pilots/asteroid-surfer.png";
	private const string SynthSingerIconPath = "res://assets/ui/pilots/synth-singer.png";
	private const string PositiveQuirkIconPath = "res://assets/ui/pilots/positive-quirk-placeholder.svg";
	private const string NegativeQuirkIconPath = "res://assets/ui/pilots/negative-quirk-placeholder.svg";
	private const string CharismaticIconPath = "res://assets/ui/pilots/charismatic.png";
	private const string NeuroDrugAddictIconPath = "res://assets/ui/pilots/neuro-drug-addict.png";
	private const string PilotCursorPath = "res://assets/ui/turn_flow/active_cursor.svg";
	private const int SkillNodeSize = 60;
	private const int IdentityIconSize = 42;
	private const int PortraitSize = 144;
	private const int IdentityRailWidth = 204;

	private static readonly Color AvailableColor = HudPalettes.PurpleRed.Primary;
	private static readonly Color LearnedColor = new(0.42f, 0.88f, 0.58f);
	private static readonly Color LockedColor = new(0.32f, 0.38f, 0.46f);
	private static readonly Color IdentityIconColor = HudStyles.TextColor(HudTextRole.Body);
	private static readonly Texture2D IconBorder = GD.Load<Texture2D>(IconBorderPath);
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
		_shell.Open("Character Sheet", pilot.Name);
		_shell.SetHeader(
			HudHeaderMode.Back,
			() => FleetManagementRequested?.Invoke(pilot.Id),
			"Fleet");
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
		portraitRow.AddChild(new UiFramedPortrait(
			ShipPilotPortraitCatalog.TextureFor(pilot.PortraitId))
		{
			SizeOverride = new UiSizeOverride(PortraitSize, PortraitSize),
			SizeFlagsHorizontal = SizeFlags.ShrinkCenter,
		});
		portraitRow.AddChild(BuildPilotCursor(1, canCyclePilots));
		rail.AddChild(portraitRow);

		var level = new Label
		{
			Text = $"LEVEL {pilot.ClaimedLevel}",
			HorizontalAlignment = HorizontalAlignment.Center,
			MouseFilter = MouseFilterEnum.Ignore,
		};
		HudStyles.ApplyTextRole(level, HudTextRole.Metadata);
		rail.AddChild(level);

		var background = PilotBackgroundCatalog.Get(pilot.BackgroundId);
		var identityIcons = new HBoxContainer
		{
			Alignment = BoxContainer.AlignmentMode.Begin,
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
		};
		identityIcons.AddThemeConstantOverride("separation", 8);
		identityIcons.AddChild(BuildIdentityIcon(
			BackgroundIconPathFor(background.Id),
			IdentityIconColor,
			$"Background: {background.Name}\n{background.Description}"));
		identityIcons.AddChild(BuildPilotClassIcon(pilot));
		if (pilot.PositiveQuirkId is { } positiveQuirkId)
		{
			var positiveQuirk = PilotQuirkCatalog.Get(positiveQuirkId);
			identityIcons.AddChild(BuildIdentityIcon(
				QuirkIconPathFor(positiveQuirk.Id),
				PositiveQuirkColor,
				$"Positive Quirk: {positiveQuirk.Name}\n{positiveQuirk.Description}"));
		}

		if (pilot.NegativeQuirkId is { } negativeQuirkId)
		{
			var negativeQuirk = PilotQuirkCatalog.Get(negativeQuirkId);
			identityIcons.AddChild(BuildIdentityIcon(
				QuirkIconPathFor(negativeQuirk.Id),
				NegativeQuirkColor,
				$"Negative Quirk: {negativeQuirk.Name}\n{negativeQuirk.Description}"));
		}

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
			rail.AddChild(BuildMeter(
				"HULL",
				assignedShip.HullPoints,
				assignedShip.Loadout.MaxHullPoints,
				new Color(0.85f, 0.15f, 0.18f)));
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

	private static Button BuildPilotClassIcon(Pilot pilot)
	{
		var iconPath = pilot.ClassId switch
		{
			GrimSpace.Units.Enums.EOperatorClassId.SpaceRanger => SpaceRangerClassIconPath,
			GrimSpace.Units.Enums.EOperatorClassId.Support => SupportClassIconPath,
			_ => throw new ArgumentOutOfRangeException(nameof(pilot), pilot.ClassId, null),
		};
		return BuildIdentityIcon(iconPath, IdentityIconColor, $"Class: {pilot.ClassId.DisplayName()}");
	}

	private static Button BuildIdentityIcon(
		string iconPath,
		Color iconColor,
		string? tooltip,
		int size = IdentityIconSize,
		int iconSize = 24)
	{
		var button = new Button
		{
			CustomMinimumSize = new Vector2(size, size),
			Icon = SvgIconLoader.Load(iconPath, iconColor, iconSize),
			ExpandIcon = false,
			IconAlignment = HorizontalAlignment.Center,
			VerticalIconAlignment = VerticalAlignment.Center,
			FocusMode = FocusModeEnum.None,
			MouseDefaultCursorShape = CursorShape.Arrow,
		};
		if (!string.IsNullOrEmpty(tooltip))
			HudTooltipCard.SetTooltip(button, tooltip);
		ApplyIconBorder(button, 1f);
		return button;
	}

	private static void ApplyIconBorder(Button button, float brightness)
	{
		button.AddThemeStyleboxOverride("normal", IconBorderStyle(brightness));
		button.AddThemeStyleboxOverride("hover", IconBorderStyle(brightness * 1.25f));
		button.AddThemeStyleboxOverride("pressed", IconBorderStyle(brightness));
		button.AddThemeStyleboxOverride("disabled", IconBorderStyle(brightness));
	}

	private static StyleBoxTexture IconBorderStyle(float brightness) =>
		new()
		{
			Texture = IconBorder,
			ModulateColor = new Color(brightness, brightness, brightness),
		};

	private Control BuildPilotCursor(int direction, bool visible)
	{
		const int cursorWidth = 20;
		const int cursorHeight = 16;
		var host = new Control
		{
			CustomMinimumSize = new Vector2(cursorWidth, PortraitSize),
			Visible = visible,
			MouseFilter = MouseFilterEnum.Stop,
			MouseDefaultCursorShape = CursorShape.PointingHand,
		};
		HudTooltipCard.SetTooltip(host, direction < 0 ? "Previous pilot" : "Next pilot");
		var iconHost = new Control
		{
			CustomMinimumSize = new Vector2(cursorWidth, cursorHeight),
			Position = new Vector2(0f, (PortraitSize - cursorHeight) * 0.5f),
			MouseFilter = MouseFilterEnum.Ignore,
			PivotOffset = new Vector2(cursorWidth * 0.5f, cursorHeight * 0.5f),
			RotationDegrees = direction < 0 ? -90f : 90f,
		};
		iconHost.AddChild(new TextureRect
		{
			Texture = ResourceLoader.Load<Texture2D>(PilotCursorPath),
			CustomMinimumSize = new Vector2(cursorWidth, cursorHeight),
			Size = new Vector2(cursorWidth, cursorHeight),
			ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
			StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
			MouseFilter = MouseFilterEnum.Ignore,
		});
		host.AddChild(iconHost);
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

		var definitions = PilotSkillAvailability.TreeFor(pilot)
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

		var host = new HudTooltipHost
		{
			CustomMinimumSize = new Vector2(SkillNodeSize, SkillNodeSize),
			MouseFilter = MouseFilterEnum.Stop,
		};
		host.SetTooltip(definition.Tooltip);

		var button = new Button
		{
			CustomMinimumSize = new Vector2(SkillNodeSize, SkillNodeSize),
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
			SizeFlagsVertical = SizeFlags.ExpandFill,
			Disabled = !canLearn,
			Icon = SvgIconLoader.Load(IconPathFor(definition.Id), color, 34),
			ExpandIcon = false,
			IconAlignment = HorizontalAlignment.Center,
			VerticalIconAlignment = VerticalAlignment.Center,
			MouseFilter = MouseFilterEnum.Ignore,
		};
		button.AddThemeColorOverride("icon_disabled_color", Colors.White);
		ApplyIconBorder(button, isLearned || canLearn ? 1f : 0.5f);
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

	private static string IconPathFor(EPilotSkillId skillId) =>
		skillId switch
		{
			EPilotSkillId.ShieldsUp => ShieldIconPath,
			EPilotSkillId.OverchargeEngines => OverchargeEnginesIconPath,
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
			EPilotBackgroundId.ScrapDrone => ScrapDroneBackgroundIconPath,
			EPilotBackgroundId.OptimalityEnforcer => OptimalityEnforcerIconPath,
			EPilotBackgroundId.CorporateSlave => CorporateSlaveIconPath,
			EPilotBackgroundId.PodRacer => PodRacerIconPath,
			EPilotBackgroundId.AsteroidSurfer => AsteroidSurferIconPath,
			EPilotBackgroundId.SynthSinger => SynthSingerIconPath,
			_ => throw new ArgumentOutOfRangeException(nameof(backgroundId), backgroundId, null),
		};

	private static string QuirkIconPathFor(EPilotQuirkId quirkId) =>
		quirkId switch
		{
			EPilotQuirkId.Charismatic => CharismaticIconPath,
			EPilotQuirkId.NeuroDrugAddict => NeuroDrugAddictIconPath,
			_ => throw new ArgumentOutOfRangeException(nameof(quirkId), quirkId, null),
		};
}
