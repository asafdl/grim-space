using GrimSpace.World.StarSystem;
using GrimSpace.World.StarSystem.Generation;
using GrimSpace.World.StarSystem.Merchants;
using GrimSpace.World.StarSystem.Poi;
using GrimSpace.World.StarSystem.Poi.Concrete;

namespace GrimSpace.Tests.World.StarSystem.Poi;

internal static class MapFacilityOperators
{
	public static string ShopOperatorName(StarMap map) =>
		MerchantOperatorName(
			map,
			SupplySystemPlan.Copper.TradeHubPoiId,
			TradeHub.DockyardFacilitySlug,
			EMerchantCatalog.Weapons);

	public static string ShieldOperatorName(StarMap map) =>
		MerchantOperatorName(
			map,
			SupplySystemPlan.Copper.TradeHubPoiId,
			TradeHub.DockyardFacilitySlug,
			EMerchantCatalog.ShipSupport);

	public static string MarketOperatorName(StarMap map) =>
		OperatorName(
			map,
			SupplySystemPlan.Copper.TradeHubPoiId,
			TradeHub.MarketFacilitySlug,
			EFacilityOperatorRole.Dialog);

	public static string ContractOperatorName(StarMap map) =>
		OperatorName(
			map,
			SupplySystemPlan.Copper.AdministrativePoiId,
			AdministrativeCore.ManagementFacilitySlug,
			EFacilityOperatorRole.Contracts);

	public static string MineContractOperatorName(StarMap map) =>
		OperatorName(
			map,
			SupplySystemPlan.Copper.ExtractionPoiId,
			OreMine.MineFacilitySlug,
			EFacilityOperatorRole.Contracts);

	public static string WarehouseManagerOperatorName(StarMap map) =>
		OperatorName(
			map,
			SupplySystemPlan.Copper.StoragePoiId,
			StorageFacility.WarehouseFacilitySlug,
			EFacilityOperatorRole.Dialog);

	public static string RefineryOperatorName(StarMap map) =>
		OperatorName(
			map,
			SupplySystemPlan.Copper.RefineryPoiId,
			Refinery.RefineryFacilitySlug,
			EFacilityOperatorRole.Dialog);

	public static string TravelOperatorName(StarMap map) =>
		OperatorName(
			map,
			SupplySystemPlan.Copper.ExitPoiId,
			Wormhole.TravelFacilitySlug,
			EFacilityOperatorRole.Dialog);

	private static string MerchantOperatorName(
		StarMap map,
		string poiId,
		string facilitySlug,
		EMerchantCatalog catalog)
	{
		var facilityId = Facility.ScopedId(poiId, facilitySlug);
		var poi = map.PointsOfInterest.First(p => p.Id == poiId);
		var facility = poi.Facilities.First(f => f.Id == facilityId);
		return facility.Operators.First(op =>
			op.Role == EFacilityOperatorRole.Merchant && op.MerchantCatalog == catalog).Name;
	}

	private static string OperatorName(StarMap map, string poiId, string facilitySlug, EFacilityOperatorRole role)
	{
		var facilityId = Facility.ScopedId(poiId, facilitySlug);
		var poi = map.PointsOfInterest.First(p => p.Id == poiId);
		var facility = poi.Facilities.First(f => f.Id == facilityId);
		return facility.Operators.First(op => op.Role == role).Name;
	}
}
