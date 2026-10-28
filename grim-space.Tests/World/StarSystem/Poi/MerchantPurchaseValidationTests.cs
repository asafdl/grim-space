using GrimSpace.Core.Engine;
using GrimSpace.World.StarSystem;
using GrimSpace.World.StarSystem.Actions;
using GrimSpace.World.StarSystem.Contracts;
using GrimSpace.World.StarSystem.Contracts.Objectives;
using GrimSpace.World.StarSystem.Generation;
using GrimSpace.World.StarSystem.Merchants;
using GrimSpace.World.StarSystem.Poi;
using GrimSpace.World.StarSystem.Poi.Concrete;
using GrimSpace.Tests.World.StarSystem.Contracts;

namespace GrimSpace.Tests.World.StarSystem.Poi;

[StarSystemTestSuite]
public sealed class MerchantPurchaseValidationTests(StarMapFixture maps)
{
	[Fact]
	public void TradeHubGraceMerchant_RequiresExactResolvedSubtype()
	{
		var map = maps.Fresh(42);
		var hub = map.GetPointOfInterest(SupplySystemPlan.Copper.TradeHubPoiId);
		var dockyard = hub.GetFacility(Facility.ScopedId(
			hub.Id,
			TradeHub.DockyardFacilitySlug));
		var merchantName = MapFacilityOperators.ShopOperatorName(map);

		Assert.True(MerchantPurchaseValidation.OperatorServesMerchantSubtype(
			map,
			hub.Id,
			dockyard.Id,
			merchantName,
			EMerchantSubtype.Ships));
		Assert.False(MerchantPurchaseValidation.OperatorServesMerchantSubtype(
			map,
			hub.Id,
			dockyard.Id,
			merchantName,
			EMerchantSubtype.Pilots));
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
				EMerchantSubtype.Ships),
			EOperatorAssignmentPriority.Merchant,
			"merchant-board:ships:0");

		Assert.True(MerchantPurchaseValidation.OperatorServesMerchantSubtype(
			map,
			poi.Id,
			facility.Id,
			facilityOperator.Name,
			EMerchantSubtype.Ships));
	}

	[Fact]
	public void HigherPriorityAssignment_SuspendsMerchantPurchaseAuthority()
	{
		var map = maps.Fresh(42);
		var hub = map.GetPointOfInterest(SupplySystemPlan.Copper.TradeHubPoiId);
		var dockyard = hub.GetFacility(Facility.ScopedId(
			hub.Id,
			TradeHub.DockyardFacilitySlug));
		var merchantName = MapFacilityOperators.ShopOperatorName(map);
		hub.OperatorTemporaryRoles.GrantPrimary(
			dockyard.Id,
			merchantName,
			EFacilityOperatorRole.DeliveryTurnIn,
			"delivery-1");

		Assert.False(MerchantPurchaseValidation.OperatorServesMerchantSubtype(
			map,
			hub.Id,
			dockyard.Id,
			merchantName,
			EMerchantSubtype.Ships));
	}

	[Fact]
	public void LeadSaleEndpoint_RequiresActivePresentation()
	{
		var map = maps.Fresh(42);
		var contract = ContractFactory.Build(
			map,
			"lead-validation",
			EContractKind.Lead,
			new LeadCreateArgs(
				ELeadKind.BlackMarketSale,
				new ContractNarrative("Quiet Opportunity", "A seller is available."),
				300,
				SpaceMeetingChance: 0));
		Assert.True(map.ContractRegistry.TryAdd(contract));
		var unitId = map.FleetRegistry.All.First().State.Id;
		var runtimes = new ActorRuntimes<GrimSpace.World.StarSystem.Runtime.ActorRuntime>();
		runtimes.For(unitId);
		var engine = new Engine<StarMap, GrimSpace.World.StarSystem.Runtime.ActorRuntime>(map, runtimes);
		engine.Commit(ContractActionTestContext.Accept(engine.World, unitId, contract.Id));
		var sale = (BlackMarketSaleObjective)contract.Objective;

		Assert.True(MerchantPurchaseValidation.EndpointServesMerchantSubtype(
			engine.World,
			new LeadSaleMerchantEndpoint(contract.Id),
			sale.MerchantSubtype));
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
			EMerchantSubtype.Ships));
	}
}
