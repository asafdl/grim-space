using GrimSpace.Run;
using GrimSpace.World.StarSystem;
using GrimSpace.World.StarSystem.Contracts;
using GrimSpace.World.StarSystem.Encounter;
using GrimSpace.World.StarSystem.Contracts.Objectives;
using GrimSpace.World.StarSystem.Poi;
using GrimSpace.World.StarSystem.Poi.Concrete;
using GrimSpace.Tests.World.StarSystem;
using GrimSpace.Tests.World.StarSystem.Poi;

namespace GrimSpace.Tests.World.StarSystem.Contracts;

[StarSystemTestSuite]
public sealed class DeliveryMapIndicatorsTests(StarMapFixture maps)
{
	[Fact]
	public void CountActiveByPresenterPoi_IncludesCurrentFacilityTarget()
	{
		var map = maps.Fresh(42);
		var contract = CreateFacilityDelivery(map);
		var objective = Assert.IsType<DeliveryObjective>(contract.Objective);
		var leg = Assert.IsType<FacilityDeliveryLeg>(objective.Route.Legs[0]);
		var poi = map.GetPointOfInterest(leg.PoiId);
		poi.OperatorTemporaryRoles.GrantPrimary(
			leg.FacilityId,
			leg.OperatorName,
			EFacilityOperatorRole.DeliveryTurnIn,
			contract.Id);

		Assert.Empty(DeliveryMapIndicators.CountActiveByPresenterPoi(map));

		Assert.True(map.ContractRegistry.Activate(new DeliveryContractState(
			contract.Id,
			EContractStatus.Active,
			0,
			State.PlayerFleetUnitId,
			objective.Route.Legs.Count)));

		var counts = DeliveryMapIndicators.CountActiveByPresenterPoi(map);

		Assert.Equal(1, counts[poi.Id]);
		Assert.Equal("Delivery", DeliveryMapIndicators.TooltipForPoi(map, poi.Id));
		Assert.True(DeliveryMapIndicators.HasActiveAtFacility(map, poi, leg.FacilityId));
		Assert.Empty(DeliveryMapIndicators.ActiveMeetings(map));
	}

	[Fact]
	public void ActiveMeetings_IncludesCurrentSpaceTarget()
	{
		var map = maps.Fresh(42);
		var contract = ContractFactory.Build(
			map,
			"delivery-meeting-indicator",
			EContractKind.Delivery,
			new DeliveryCreateArgs(
				EDangerLevel.Moderate,
				ContractNarrative.ForDelivery("Delivery", "Cargo.", "Received."),
				PickupPoiId: map.Blueprint.SupplyPlan.StoragePoiId,
				Generation: new DeliveryGenerationConfig(3, spaceMeetingChance: 1.0)));
		Assert.True(map.ContractRegistry.TryAdd(contract));
		var objective = Assert.IsType<DeliveryObjective>(contract.Objective);
		var meeting = Assert.IsType<SpaceMeetingDeliveryLeg>(objective.Route.Legs[0]);
		Assert.True(map.ContractRegistry.Activate(new DeliveryContractState(
			contract.Id,
			EContractStatus.Active,
			0,
			State.PlayerFleetUnitId,
			objective.Route.Legs.Count)));

		var markers = DeliveryMapIndicators.ActiveMeetings(map);

		var marker = Assert.Single(markers);
		Assert.Equal(meeting.MeetingId, marker.MeetingId);
		Assert.Equal(meeting.Position, marker.Position);
		Assert.Empty(DeliveryMapIndicators.CountActiveByPresenterPoi(map));
	}

	private static Contract CreateFacilityDelivery(StarMap map)
	{
		var plan = map.Blueprint.SupplyPlan;
		return ContractFactory.Create(
			map,
			EContractKind.Delivery,
			new DeliveryCreateArgs(
				EDangerLevel.VeryLow,
				ContractNarrative.ForDelivery("Test Delivery", "Pick up here.", "Drop off there."),
				PickupPoiId: plan.StoragePoiId,
				DropoffPoiId: plan.ExitPoiId,
				DropoffFacilityId: Facility.ScopedId(plan.ExitPoiId, Wormhole.TravelFacilitySlug),
				DropoffOperatorName: MapFacilityOperators.TravelOperatorName(map)));
	}
}
