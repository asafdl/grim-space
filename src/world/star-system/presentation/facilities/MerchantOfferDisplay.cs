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
