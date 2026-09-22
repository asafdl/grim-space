using GrimSpace.World.StarSystem;
using GrimSpace.World.StarSystem.Generation;
using GrimSpace.World.StarSystem.Poi;
using GrimSpace.World.StarSystem.Poi.Concrete;

namespace GrimSpace.Tests.World.StarSystem.Poi;

internal static class MapFacilityOperators
{
	public static string ShopOperatorName(StarMap map) =>
		OperatorName(map, SupplySystemPlan.Copper.TradeHubPoiId, TradeHub.DockyardFacilitySlug, EFacilityOperatorRole.DockyardShop);

	public static string ShieldOperatorName(StarMap map) =>
		OperatorName(map, SupplySystemPlan.Copper.TradeHubPoiId, TradeHub.DockyardFacilitySlug, EFacilityOperatorRole.ShieldRecharge);

	public static string ContractOperatorName(StarMap map) =>
		OperatorName(
			map,
			SupplySystemPlan.Copper.AdministrativePoiId,
			AdministrativeCore.ManagementFacilitySlug,
			EFacilityOperatorRole.Contracts);

	private static string OperatorName(StarMap map, string poiId, string facilitySlug, EFacilityOperatorRole role)
	{
		var facilityId = Facility.ScopedId(poiId, facilitySlug);
		var poi = map.PointsOfInterest.First(p => p.Id == poiId);
		var facility = poi.Facilities.First(f => f.Id == facilityId);
		return facility.Operators.First(op => op.Role == role).Name;
	}
}
