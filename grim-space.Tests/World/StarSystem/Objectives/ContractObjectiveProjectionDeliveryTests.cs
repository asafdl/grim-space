using GrimSpace.Tutorials;
using GrimSpace.World.StarSystem;
using GrimSpace.World.StarSystem.Contracts;
using GrimSpace.World.StarSystem.Contracts.Objectives;
using GrimSpace.World.StarSystem.Objectives;
using GrimSpace.Tests.World.StarSystem;

namespace GrimSpace.Tests.World.StarSystem.Objectives;

[StarSystemTestSuite]
public sealed class ContractObjectiveProjectionDeliveryTests(StarMapFixture maps)
{
	[Fact]
	public void Project_ActiveDeliveryContract_PointsAtDropoffOnly()
	{
		var map = maps.Fresh(42);
		TutorialBeatContracts.OfferBeatB(map);
		var contract = map.ContractRegistry.Pending.Single();
		var delivery = (DeliveryObjective)contract.Objective;
		var dropoff = map.PointsOfInterest.First(poi => poi.Id == delivery.TurnInPoiId);
		var facility = dropoff.GetFacility(delivery.TurnInFacilityId);

		var objective = ContractObjectiveProjection.Project(map, contract);

		Assert.Equal("Supply Run ★", objective.Title);
		var near = Assert.IsType<ObjectiveSummaryContent.NearLandmark>(objective.Summary);
		Assert.Equal($"Deliver cargo to \"{delivery.TurnInOperatorName}\" at {facility.DisplayName} in ", near.Prefix);
		Assert.Equal(dropoff.Id, near.LandmarkPoiId);
		Assert.Equal(dropoff.DisplayName, near.LandmarkDisplayName);
		Assert.Equal(".", near.Suffix);
	}
}
