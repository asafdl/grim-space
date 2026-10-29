using Godot;
using GrimSpace.Components;
using GrimSpace.Pilots;
using GrimSpace.Run;
using GrimSpace.Units;
using GrimSpace.Units.Enums;

namespace GrimSpace.World.StarSystem.Presentation.Ui;

public static class PilotIdentityIconRail
{
	private const string IconBorderPath = "res://assets/ui/components/runtime/icon_border_blue.png";
	private const string ScrapDroneBackgroundIconPath = "res://assets/ui/pilots/scrap-drone.png";
	private const string SpaceRangerClassIconPath = "res://assets/ui/classes/space-ranger.png";
	private const string SupportClassIconPath = "res://assets/ui/classes/support.png";
	private const string OptimalityEnforcerIconPath = "res://assets/ui/pilots/optimality-enforcer.png";
	private const string CorporateSlaveIconPath = "res://assets/ui/pilots/corporate-slave.png";
	private const string PodRacerIconPath = "res://assets/ui/pilots/pod-racer.png";
	private const string AsteroidSurferIconPath = "res://assets/ui/pilots/asteroid-surfer.png";
	private const string SynthSingerIconPath = "res://assets/ui/pilots/synth-singer.png";
	private const string CharismaticIconPath = "res://assets/ui/pilots/charismatic.png";
	private const string NeuroDrugAddictIconPath = "res://assets/ui/pilots/neuro-drug-addict.png";

	private const int IconSlotSize = 42;
	private const int IconSize = 24;

	private static readonly Color IdentityIconColor = HudStyles.TextColor(HudTextRole.Body);
	private static readonly Texture2D IconBorder = GD.Load<Texture2D>(IconBorderPath);
	private static readonly Color PositiveQuirkColor = HudPalettes.Gold.Primary;
	private static readonly Color NegativeQuirkColor = HudStyles.TextColor(HudTextRole.Danger);

	public static Control BuildFor(Pilot pilot) =>
		Build(
			pilot.BackgroundId,
			pilot.ClassId,
			pilot.PositiveQuirkId,
			pilot.NegativeQuirkId);

	public static Control BuildFor(PilotRecruitProfile profile) =>
		Build(
			profile.BackgroundId,
			profile.ClassId,
			profile.PositiveQuirkId,
			profile.NegativeQuirkId);

	private static Control Build(
		EPilotBackgroundId backgroundId,
		EOperatorClassId classId,
		EPilotQuirkId? positiveQuirkId,
		EPilotQuirkId? negativeQuirkId)
	{
		var background = PilotBackgroundCatalog.Get(backgroundId);
		var row = new HBoxContainer
		{
			Alignment = BoxContainer.AlignmentMode.Begin,
			MouseFilter = Control.MouseFilterEnum.Pass,
		};
		row.AddThemeConstantOverride("separation", 8);
		row.AddChild(CreateIcon(
			BackgroundIconPathFor(backgroundId),
			IdentityIconColor,
			$"Background: {background.Name}\n{background.Description}"));
		row.AddChild(CreateClassIcon(classId));
		if (positiveQuirkId is { } positiveId)
		{
			var positiveQuirk = PilotQuirkCatalog.Get(positiveId);
			row.AddChild(CreateIcon(
				QuirkIconPathFor(positiveId),
				PositiveQuirkColor,
				$"Positive Quirk: {positiveQuirk.Name}\n{positiveQuirk.Description}"));
		}

		if (negativeQuirkId is { } negativeId)
		{
			var negativeQuirk = PilotQuirkCatalog.Get(negativeId);
			row.AddChild(CreateIcon(
				QuirkIconPathFor(negativeId),
				NegativeQuirkColor,
				$"Negative Quirk: {negativeQuirk.Name}\n{negativeQuirk.Description}"));
		}

		return row;
	}

	private static Control CreateClassIcon(EOperatorClassId classId)
	{
		var iconPath = classId switch
		{
			EOperatorClassId.SpaceRanger => SpaceRangerClassIconPath,
			EOperatorClassId.Support => SupportClassIconPath,
			_ => throw new ArgumentOutOfRangeException(nameof(classId), classId, null),
		};
		return CreateIcon(
			iconPath,
			IdentityIconColor,
			$"Class: {classId.DisplayName()}");
	}

	private static HudTooltipHost CreateIcon(string iconPath, Color iconColor, string tooltip)
	{
		var host = new HudTooltipHost
		{
			CustomMinimumSize = new Vector2(IconSlotSize, IconSlotSize),
			MouseFilter = Control.MouseFilterEnum.Stop,
		};
		host.SetTooltip(tooltip);
		host.GuiInput += @event =>
		{
			if (@event is InputEventMouseButton { Pressed: true })
				host.AcceptEvent();
		};

		var button = new Button
		{
			CustomMinimumSize = new Vector2(IconSlotSize, IconSlotSize),
			Icon = SvgIconLoader.Load(iconPath, iconColor, IconSize),
			ExpandIcon = false,
			IconAlignment = HorizontalAlignment.Center,
			VerticalIconAlignment = VerticalAlignment.Center,
			FocusMode = Control.FocusModeEnum.None,
			MouseFilter = Control.MouseFilterEnum.Ignore,
			MouseDefaultCursorShape = Control.CursorShape.Arrow,
		};
		ApplyIconBorder(button, 1f);
		host.AddChild(button);
		return host;
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
