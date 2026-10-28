using GrimSpace.Math.Grid;
using GrimSpace.World.StarSystem.Merchants;

namespace GrimSpace.World.StarSystem.Presentation.Facilities;

internal static class MerchantOfferDisplay
{
	public static string HullRepairBody(int current, int max) =>
		$"Hull integrity {current} \u2192 {max}";

	public static string ShieldRechargeBody(int current, int max) =>
		$"Shield charge {current} \u2192 {max}";

	public static string RechargeAllBody(int current, int max) =>
		$"Total shields {current} \u2192 {max}";

	public static string BandLabel(EMerchantAvailabilityBand band) =>
		band switch
		{
			EMerchantAvailabilityBand.T0 => "Shop tier T0",
			EMerchantAvailabilityBand.T0_5 => "Shop tier T0.5",
			EMerchantAvailabilityBand.T1 => "Shop tier T1",
			EMerchantAvailabilityBand.T1_5 => "Shop tier T1.5",
			EMerchantAvailabilityBand.T2 => "Shop tier T2",
			_ => throw new ArgumentOutOfRangeException(nameof(band), band, null),
		};

	public static string FaceLabel(ESpatialOrientation face) =>
		face switch
		{
			ESpatialOrientation.Forward => "Forward",
			ESpatialOrientation.Retro => "Aft",
			ESpatialOrientation.Port => "Port",
			ESpatialOrientation.Starboard => "Starboard",
			ESpatialOrientation.Dorsal => "Dorsal",
			ESpatialOrientation.Ventral => "Ventral",
			_ => face.ToString(),
		};
}
