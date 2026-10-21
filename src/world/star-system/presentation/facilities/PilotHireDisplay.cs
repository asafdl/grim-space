using GrimSpace.Pilots;
using GrimSpace.Units.Enums;
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
}
