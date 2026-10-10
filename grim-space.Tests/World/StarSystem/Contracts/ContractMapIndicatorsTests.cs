using GrimSpace.World.StarSystem;
using GrimSpace.World.StarSystem.Contracts;
using GrimSpace.World.StarSystem.Poi;
using GrimSpace.Tests.World.StarSystem;

namespace GrimSpace.Tests.World.StarSystem.Contracts;

[StarSystemTestSuite]
public sealed class ContractMapIndicatorsTests(StarMapFixture maps)
{
	[Fact]
	public void CountPendingByPresenterPoi_IncludesAssignedPendingContract()
	{
		var map = maps.FreshWithBeatAHunt(42);
		var poiId = PresentPendingContract(map);
		var counts = ContractMapIndicators.CountPendingByPresenterPoi(map);

		Assert.Equal(1, counts[poiId]);
	}

	[Fact]
	public void CountPendingByIssuerPoi_OmitsPoiAfterContractRejected()
	{
		var map = maps.FreshWithBeatAHunt(42);
		var contractId = map.ContractRegistry.Pending.First().Id;
		PresentPendingContract(map);
		map.ContractRegistry.Activate(new ContractState(
			contractId,
			EContractStatus.Rejected,
			null,
			null));

		var counts = ContractMapIndicators.CountPendingByPresenterPoi(map);

		Assert.Empty(counts);
	}

	[Fact]
	public void TooltipForCount_UsesAvailablePhrase()
	{
		Assert.Equal("1 available", ContractMapIndicators.TooltipForCount(1));
		Assert.Equal("3 available", ContractMapIndicators.TooltipForCount(3));
	}

	[Fact]
	public void TooltipForPoi_ListsAssignedPendingContractsByKind()
	{
		var map = maps.FreshWithBeatAHunt(42);
		var poiId = PresentPendingContract(map);

		Assert.Equal("1x Hunt", ContractMapIndicators.TooltipForPoi(map, poiId));
	}

	private static string PresentPendingContract(StarMap map)
	{
		var poi = map.PointsOfInterest.First(candidate => candidate.Facilities.Count > 0);
		var facility = poi.Facilities[0];
		var facilityOperator = facility.Operators[0];
		poi.OperatorTemporaryRoles.Grant(
			facility.Id,
			facilityOperator.Name,
			EFacilityOperatorRole.Contracts,
			map.ContractRegistry.Pending.Single().Id,
			acceptsSourcesUntilTick: 10);
		return poi.Id;
	}
}
