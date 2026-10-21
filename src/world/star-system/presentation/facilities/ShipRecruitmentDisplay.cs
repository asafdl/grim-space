using GrimSpace.Units.Enums;
using GrimSpace.World.StarSystem.Merchants;

namespace GrimSpace.World.StarSystem.Presentation.Facilities;

internal static class ShipRecruitmentDisplay
{
	public static string ListMetadata(RecruitmentOffer offer) =>
		$"{SalesPitch(offer.Chassis)} · {offer.GearTier}";

	public static string SalesPitch(EType chassis) =>
		chassis switch
		{
			EType.RepurposedMiner =>
				"The workhorse the Syndicate relied on for centuries.",
			EType.Gunship =>
				"A bit hard to manuever, but packs a punch. Great gun support.",
			_ => "Fleet-ready hull from the broker's current manifest.",
		};
}
