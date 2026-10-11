using GrimSpace.World.StarSystem.Generation;
using GrimSpace.World.StarSystem.Merchants;
using GrimSpace.World.StarSystem.Poi;
using GrimSpace.World.StarSystem.Poi.Concrete;

namespace GrimSpace.Tests.World.StarSystem.Poi;

[StarSystemTestSuite]
public sealed class MerchantPurchaseValidationTests(StarMapFixture maps)
{
	[Fact]
	public void StaticMerchant_RequiresExactResolvedSubtype()
	{
		var map = maps.Fresh(42);
		var hub = map.GetPointOfInterest(SupplySystemPlan.Copper.TradeHubPoiId);
		var dockyard = hub.GetFacility(Facility.ScopedId(
			hub.Id,
			TradeHub.DockyardFacilitySlug));
		var merchant = dockyard.Operators.Single(candidate =>
			candidate.MerchantCatalog == EMerchantCatalog.Weapons);

		Assert.True(MerchantPurchaseValidation.OperatorServesMerchantSubtype(
			map,
			hub.Id,
			dockyard.Id,
			merchant.Name,
			EMerchantSubtype.Weapons));
		Assert.False(MerchantPurchaseValidation.OperatorServesMerchantSubtype(
			map,
			hub.Id,
			dockyard.Id,
			merchant.Name,
			EMerchantSubtype.ShipSupport));
	}

	[Fact]
	public void TemporaryMerchant_UsesResolvedAssignment()
	{
		var map = maps.Fresh(42);
		var poi = map.GetPointOfInterest(SupplySystemPlan.Copper.AdministrativePoiId);
		var facility = poi.GetFacility(Facility.ScopedId(
			poi.Id,
			AdministrativeCore.ManagementFacilitySlug));
		var facilityOperator = facility.Operators.Single(candidate =>
			candidate.Role == EFacilityOperatorRole.Dialog);
		poi.OperatorTemporaryRoles.Grant(
			facility.Id,
			facilityOperator.Name,
			new OperatorInteraction(
				EFacilityOperatorRole.Merchant,
				EMerchantSubtype.ShipSupport),
			EOperatorAssignmentPriority.Merchant,
			"merchant-board:ship-support:0");

		Assert.True(MerchantPurchaseValidation.OperatorServesMerchantSubtype(
			map,
			poi.Id,
			facility.Id,
			facilityOperator.Name,
			EMerchantSubtype.ShipSupport));
	}

	[Fact]
	public void HigherPriorityAssignment_SuspendsMerchantPurchaseAuthority()
	{
		var map = maps.Fresh(42);
		var hub = map.GetPointOfInterest(SupplySystemPlan.Copper.TradeHubPoiId);
		var dockyard = hub.GetFacility(Facility.ScopedId(
			hub.Id,
			TradeHub.DockyardFacilitySlug));
		var merchant = dockyard.Operators.Single(candidate =>
			candidate.MerchantCatalog == EMerchantCatalog.Weapons);
		hub.OperatorTemporaryRoles.GrantPrimary(
			dockyard.Id,
			merchant.Name,
			EFacilityOperatorRole.DeliveryTurnIn,
			"delivery-1");

		Assert.False(MerchantPurchaseValidation.OperatorServesMerchantSubtype(
			map,
			hub.Id,
			dockyard.Id,
			merchant.Name,
			EMerchantSubtype.Weapons));
	}

	[Fact]
	public void HealingStation_UsesSeparateAuthority()
	{
		var map = maps.Fresh(42);
		var poi = map.GetPointOfInterest(SupplySystemPlan.Copper.AdministrativePoiId);
		var facility = poi.GetFacility(Facility.ScopedId(
			poi.Id,
			AdministrativeCore.ManagementFacilitySlug));
		var healingStation = facility.Operators.Single(candidate =>
			candidate.Role == EFacilityOperatorRole.HealingStation);

		Assert.True(MerchantPurchaseValidation.OperatorServesHealingStation(
			map,
			poi.Id,
			facility.Id,
			healingStation.Name));
		Assert.False(MerchantPurchaseValidation.OperatorServesMerchantSubtype(
			map,
			poi.Id,
			facility.Id,
			healingStation.Name,
			EMerchantSubtype.ShipSupport));
	}
}
