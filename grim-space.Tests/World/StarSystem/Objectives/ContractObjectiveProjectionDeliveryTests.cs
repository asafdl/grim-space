using GrimSpace.Tutorials;
using GrimSpace.World.StarSystem;
using GrimSpace.World.StarSystem.Contracts;
using GrimSpace.World.StarSystem.Contracts.Objectives;
using GrimSpace.World.StarSystem.Objectives;
using GrimSpace.Tests.World.StarSystem;

namespace GrimSpace.Tests.World.StarSystem.Objectives;

public sealed class ContractObjectiveProjectionDeliveryTests(StarMapFixture maps)
{
	[Fact]
	public void Project_DeliveryContract_UsesRouteBetweenIssuerAndDropoff()
	{
		var map = maps.Fresh(42);
		var args = TutorialBeatContracts.CreateBeatBDeliveryArgs(map);
		var contract = ContractFactory.Create(map, EContractKind.Delivery, args);
		var delivery = (DeliveryObjective)contract.Objective;
		var issuer = map.PointsOfInterest.First(poi => poi.Id == contract.IssuerPoiId);
		var dropoff = map.PointsOfInterest.First(poi => poi.Id == delivery.TurnInPoiId);

		var objective = ContractObjectiveProjection.Project(map, contract);

		Assert.Equal("Supply Run", objective.Title);
		var route = Assert.IsType<ObjectiveSummaryContent.RouteBetweenLandmarks>(objective.Summary);
		Assert.Equal("Pick up at ", route.Prefix);
		Assert.Equal(issuer.Id, route.LandmarkAPoiId);
		Assert.Equal(issuer.DisplayName, route.LandmarkADisplayName);
		Assert.Equal(", deliver to ", route.Connector);
		Assert.Equal(dropoff.Id, route.LandmarkBPoiId);
		Assert.Equal(delivery.TurnInOperatorName, route.LandmarkBDisplayName);
		Assert.Equal(".", route.Suffix);
	}
}
