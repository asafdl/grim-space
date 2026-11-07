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
	private const string TankClassIconPath = "res://assets/ui/classes/tank.png";
	private const string RogueClassIconPath = "res://assets/ui/classes/rogue.png";
	private const string OptimalityEnforcerIconPath = "res://assets/ui/pilots/optimality-enforcer.png";
	private const string CorporateSlaveIconPath = "res://assets/ui/pilots/corporate-slave.png";
	private const string PodRacerIconPath = "res://assets/ui/pilots/pod-racer.png";
	private const string AsteroidSurferIconPath = "res://assets/ui/pilots/asteroid-surfer.png";
	private const string SynthSingerIconPath = "res://assets/ui/pilots/synth-singer.png";
	private const string RetiredMachineManagerIconPath = "res://assets/ui/pilots/retired-machine-manager.png";
	private const string CyberJunkieIconPath = "res://assets/ui/pilots/cyber-junkie.png";
	private const string HydraulicAbuserIconPath = "res://assets/ui/pilots/hydraulic-abuser.png";
	private const string BeanCounterIconPath = "res://assets/ui/pilots/bean-counter.png";
	private const string GearPsychoIconPath = "res://assets/ui/pilots/gear-psycho.png";
	private const string CharismaticIconPath = "res://assets/ui/pilots/charismatic.png";
	private const string PirateHaterIconPath = "res://assets/ui/pilots/positive-quirk-placeholder.svg";
	private const string ShieldMasterIconPath = "res://assets/ui/pilots/positive-quirk-placeholder.svg";
	private const string MetalMasterIconPath = "res://assets/ui/pilots/positive-quirk-placeholder.svg";
	private const string ICanFixThatIconPath = "res://assets/ui/pilots/positive-quirk-placeholder.svg";
	private const string TeamPlayerIconPath = "res://assets/ui/pilots/positive-quirk-placeholder.svg";
	private const string IceGearsIconPath = "res://assets/ui/pilots/positive-quirk-placeholder.svg";
	private const string RadiatorFanaticIconPath = "res://assets/ui/pilots/positive-quirk-placeholder.svg";
	private const string NeuroDrugAddictIconPath = "res://assets/ui/pilots/neuro-drug-addict.png";
	private const string BadTechnicianIconPath = "res://assets/ui/pilots/negative-quirk-placeholder.svg";
	private const string WeakWeldingIconPath = "res://assets/ui/pilots/negative-quirk-placeholder.svg";
	private const string RoughPatchJobIconPath = "res://assets/ui/pilots/negative-quirk-placeholder.svg";
	private const string TriggerHappyIconPath = "res://assets/ui/pilots/negative-quirk-placeholder.svg";
	private const string CowardIconPath = "res://assets/ui/pilots/negative-quirk-placeholder.svg";
	private const string FurnaceCoreIconPath = "res://assets/ui/pilots/negative-quirk-placeholder.svg";
	private const string CloggedVentsIconPath = "res://assets/ui/pilots/negative-quirk-placeholder.svg";

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
			pilot.PositiveQuirkIds,
			pilot.NegativeQuirkIds);

	public static Control BuildFor(PilotRecruitProfile profile) =>
		Build(
			profile.BackgroundId,
			profile.ClassId,
			profile.PositiveQuirkIds,
			profile.NegativeQuirkIds);

	private static Control Build(
		EPilotBackgroundId backgroundId,
		EOperatorClassId classId,
		IReadOnlyList<EPilotQuirkId> positiveQuirkIds,
		IReadOnlyList<EPilotQuirkId> negativeQuirkIds)
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
			background.Tooltip));
		row.AddChild(CreateClassIcon(classId));
		foreach (var positiveId in positiveQuirkIds)
		{
			var positiveQuirk = PilotQuirkCatalog.Get(positiveId);
			row.AddChild(CreateIcon(
				QuirkIconPathFor(positiveId),
				PositiveQuirkColor,
				positiveQuirk.Tooltip));
		}

		foreach (var negativeId in negativeQuirkIds)
		{
			var negativeQuirk = PilotQuirkCatalog.Get(negativeId);
			row.AddChild(CreateIcon(
				QuirkIconPathFor(negativeId),
				NegativeQuirkColor,
				negativeQuirk.Tooltip));
		}

		return row;
	}

	private static Control CreateClassIcon(EOperatorClassId classId)
	{
		var iconPath = classId switch
		{
			EOperatorClassId.SpaceRanger => SpaceRangerClassIconPath,
			EOperatorClassId.Support => SupportClassIconPath,
			EOperatorClassId.Tank => TankClassIconPath,
			EOperatorClassId.Rogue => RogueClassIconPath,
			_ => throw new ArgumentOutOfRangeException(nameof(classId), classId, null),
		};
		return CreateIcon(
			iconPath,
			IdentityIconColor,
			HudTooltipContent.Plain($"Class: {classId.DisplayName()}"));
	}

	private static HudTooltipHost CreateIcon(string iconPath, Color iconColor, HudTooltipContent tooltip)
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
			Icon = SvgIconLoader.LoadCentered(iconPath, iconColor, IconSize),
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
			EPilotBackgroundId.RetiredMachineManager => RetiredMachineManagerIconPath,
			EPilotBackgroundId.CyberJunkie => CyberJunkieIconPath,
			EPilotBackgroundId.HydraulicAbuser => HydraulicAbuserIconPath,
			EPilotBackgroundId.BeanCounter => BeanCounterIconPath,
			EPilotBackgroundId.GearPsycho => GearPsychoIconPath,
			_ => throw new ArgumentOutOfRangeException(nameof(backgroundId), backgroundId, null),
		};

	private static string QuirkIconPathFor(EPilotQuirkId quirkId) =>
		quirkId switch
		{
			EPilotQuirkId.Charismatic => CharismaticIconPath,
			EPilotQuirkId.PirateHater => PirateHaterIconPath,
			EPilotQuirkId.ShieldMaster => ShieldMasterIconPath,
			EPilotQuirkId.MetalMaster => MetalMasterIconPath,
			EPilotQuirkId.ICanFixThat => ICanFixThatIconPath,
			EPilotQuirkId.TeamPlayer => TeamPlayerIconPath,
			EPilotQuirkId.IceGears => IceGearsIconPath,
			EPilotQuirkId.RadiatorFanatic => RadiatorFanaticIconPath,
			EPilotQuirkId.NeuroDrugAddict => NeuroDrugAddictIconPath,
			EPilotQuirkId.BadTechnician => BadTechnicianIconPath,
			EPilotQuirkId.WeakWelding => WeakWeldingIconPath,
			EPilotQuirkId.RoughPatchJob => RoughPatchJobIconPath,
			EPilotQuirkId.TriggerHappy => TriggerHappyIconPath,
			EPilotQuirkId.Coward => CowardIconPath,
			EPilotQuirkId.FurnaceCore => FurnaceCoreIconPath,
			EPilotQuirkId.CloggedVents => CloggedVentsIconPath,
			_ => throw new ArgumentOutOfRangeException(nameof(quirkId), quirkId, null),
		};
}
