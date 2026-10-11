using GrimSpace.World.StarSystem;
using GrimSpace.World.StarSystem.Merchants;

namespace GrimSpace.Tests.World.StarSystem.Merchants;

internal static class MerchantCatalogTestBands
{
	internal static EMerchantAvailabilityBand PurchaseQuoteBand => EMerchantAvailabilityBand.T0;

	internal static void SetBand(StarMap map, EMerchantAvailabilityBand band)
	{
		while (MerchantAvailability.CurrentBand(map) < band)
			map.BumpDebugDangerProgression();
	}
}
