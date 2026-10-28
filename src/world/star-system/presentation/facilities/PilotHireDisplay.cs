using GrimSpace.Pilots;
using GrimSpace.Units.Enums;
using GrimSpace.World.StarSystem.Economy;
using GrimSpace.World.StarSystem.Merchants;

namespace GrimSpace.World.StarSystem.Presentation.Facilities;

internal static class PilotHireDisplay
{
	public static string CardTitle(EPilotBackgroundId backgroundId) =>
		PilotBackgroundCatalog.Get(backgroundId).Name;

	public static string CardSubtitle(PilotRecruitProfile profile) =>
		$"{profile.Name} · Lv {profile.ClaimedLevel}";

	public static string ShipPackageLine(EType chassis, EShipGearTier gearTier) =>
		$"{ShipChassisLabel(chassis)} ({gearTier})";

	public static int SalaryAmount(PilotRecruitProfile profile)
	{
		ArgumentNullException.ThrowIfNull(profile);
		return StrategicEconomyConfig.SalaryFor(
			profile.BaseSalaryCredits,
			profile.ClaimedLevel);
	}

	public static string EquipmentLine(EType chassis, EShipGearTier gearTier) =>
		$"Equipment: {ShipPackageLine(chassis, gearTier)}";

	public static bool ShouldShowRecruitedQuirkIcon(EPilotQuirkId? quirkId, EPilotQuirkPolarity polarity)
	{
		if (quirkId is not { } resolved)
			return false;

		var pool = polarity == EPilotQuirkPolarity.Positive
			? PilotQuirkCatalog.RecruitablePositive
			: PilotQuirkCatalog.RecruitableNegative;
		return pool.Contains(resolved);
	}

	public static string ShipChassisLabel(EType chassis) =>
		chassis switch
		{
			EType.RepurposedMiner => "Repurposed Miner",
			EType.Gunship => "Gunship",
			_ => chassis.ToString(),
		};

	public static string BackgroundIconPath(EPilotBackgroundId backgroundId) =>
		backgroundId switch
		{
			EPilotBackgroundId.ScrapDrone => "res://assets/ui/pilots/scrap-drone.png",
			EPilotBackgroundId.OptimalityEnforcer => "res://assets/ui/pilots/optimality-enforcer.png",
			EPilotBackgroundId.CorporateSlave => "res://assets/ui/pilots/corporate-slave.png",
			EPilotBackgroundId.PodRacer => "res://assets/ui/pilots/pod-racer.png",
			EPilotBackgroundId.AsteroidSurfer => "res://assets/ui/pilots/asteroid-surfer.png",
			EPilotBackgroundId.SynthSinger => "res://assets/ui/pilots/synth-singer.png",
			_ => throw new ArgumentOutOfRangeException(nameof(backgroundId), backgroundId, null),
		};
}
