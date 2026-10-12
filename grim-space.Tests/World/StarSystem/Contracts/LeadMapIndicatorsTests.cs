using GrimSpace.World.StarSystem;
using GrimSpace.World.StarSystem.Contracts;
using GrimSpace.World.StarSystem.Contracts.Objectives;
using GrimSpace.World.StarSystem.Poi;
using GrimSpace.Tests.World.StarSystem;

namespace GrimSpace.Tests.World.StarSystem.Contracts;

[StarSystemTestSuite]
public sealed class LeadMapIndicatorsTests(StarMapFixture maps)
{
	[Fact]
	public void CountPendingByPresenterPoi_IncludesAssignedPendingLead()
	{
		var map = maps.Fresh(42);
		var contract = ContractFactory.Build(
			map,
			"lead-map-indicator",
			EContractKind.Lead,
			new LeadCreateArgs(
				ELeadKind.BlackMarketSale,
				new ContractNarrative("Quiet Opportunity", "A seller is available."),
				300,
				SpaceMeetingChance: 0));
		Assert.True(map.ContractRegistry.TryAdd(contract));
		var poi = map.PointsOfInterest.First();
		var facility = poi.Facilities.First();
		var facilityOperator = facility.Operators.First(op => op.Role == EFacilityOperatorRole.Dialog);
		poi.OperatorTemporaryRoles.Grant(
			facility.Id,
			facilityOperator.Name,
			EFacilityOperatorRole.LeadContact,
			contract.Id);

		var counts = LeadMapIndicators.CountPendingByPresenterPoi(map);

		Assert.Equal(1, counts[poi.Id]);
		Assert.Equal("1 lead available", LeadMapIndicators.TooltipForPoi(map, poi.Id));
	}
}
