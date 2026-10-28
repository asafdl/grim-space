using GrimSpace.World.StarSystem;
using GrimSpace.World.StarSystem.Generation;
using GrimSpace.World.StarSystem.Merchants;
using GrimSpace.World.StarSystem.Poi;
using GrimSpace.World.StarSystem.Poi.Concrete;

namespace GrimSpace.Tests.World.StarSystem.Poi;

internal static class MapFacilityOperators
{
	public const string GraceMerchantSourceId = "legacy-trade-hub-merchant";

	public static string ShopOperatorName(StarMap map)
	{
		EnsureTradeHubMerchant(
			map,
			TradeHub.DockyardFacilitySlug,
			TradeHub.ShopOperatorSceneSlotId,
			EMerchantSubtype.Ships);
		return OperatorNameBySceneSlot(
			map,
			SupplySystemPlan.Copper.TradeHubPoiId,
			TradeHub.DockyardFacilitySlug,
			TradeHub.ShopOperatorSceneSlotId);
	}

	public static string ShipMerchantOperatorName(StarMap map)
	{
		EnsureTradeHubMerchant(
			map,
			TradeHub.MarketFacilitySlug,
			TradeHub.ShipMerchantSceneSlotId,
			EMerchantSubtype.Ships);
		return OperatorNameBySceneSlot(
			map,
			SupplySystemPlan.Copper.TradeHubPoiId,
			TradeHub.MarketFacilitySlug,
			TradeHub.ShipMerchantSceneSlotId);
	}

	public static string MarketOperatorName(StarMap map) =>
		OperatorNameBySceneSlot(
			map,
			SupplySystemPlan.Copper.TradeHubPoiId,
			TradeHub.MarketFacilitySlug,
			TradeHub.MarketOperatorSceneSlotId);

	public static string ContractOperatorName(StarMap map) =>
		OperatorName(
			map,
			SupplySystemPlan.Copper.AdministrativePoiId,
			AdministrativeCore.ManagementFacilitySlug,
			EFacilityOperatorRole.Dialog);

	public static string MineContractOperatorName(StarMap map) =>
		OperatorName(
			map,
			SupplySystemPlan.Copper.ExtractionPoiId,
			OreMine.MineFacilitySlug,
			EFacilityOperatorRole.Dialog);

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

	private static void EnsureTradeHubMerchant(
		StarMap map,
		string facilitySlug,
		string sceneSlotId,
		EMerchantSubtype subtype)
	{
		var poiId = SupplySystemPlan.Copper.TradeHubPoiId;
		var facilityId = Facility.ScopedId(poiId, facilitySlug);
		var poi = map.GetPointOfInterest(poiId);
		var facilityOperator = poi.GetFacility(facilityId).Operators.First(op =>
			string.Equals(op.SceneSlotId, sceneSlotId, StringComparison.Ordinal));
		if (poi.OperatorTemporaryRoles.TryGetInteraction(
			    facilityId,
			    facilityOperator.Name,
			    out var interaction)
			&& interaction.Role == EFacilityOperatorRole.Merchant
			&& interaction.MerchantSubtype == subtype)
			return;

		poi.OperatorTemporaryRoles.Grant(
			facilityId,
			facilityOperator.Name,
			new OperatorInteraction(EFacilityOperatorRole.Merchant, subtype),
			EOperatorAssignmentPriority.Merchant,
			GraceMerchantSourceId);
	}

	private static string OperatorNameBySceneSlot(
		StarMap map,
		string poiId,
		string facilitySlug,
		string sceneSlotId)
	{
		var facilityId = Facility.ScopedId(poiId, facilitySlug);
		var poi = map.PointsOfInterest.First(p => p.Id == poiId);
		var facility = poi.Facilities.First(f => f.Id == facilityId);
		return facility.Operators.First(op =>
			string.Equals(op.SceneSlotId, sceneSlotId, StringComparison.Ordinal)).Name;
	}

	private static string OperatorName(StarMap map, string poiId, string facilitySlug, EFacilityOperatorRole role)
	{
		var facilityId = Facility.ScopedId(poiId, facilitySlug);
		var poi = map.PointsOfInterest.First(p => p.Id == poiId);
		var facility = poi.Facilities.First(f => f.Id == facilityId);
		return facility.Operators.First(op => op.Role == role).Name;
	}
}
