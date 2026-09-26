using GrimSpace.Tutorials;
using GrimSpace.World.StarSystem;
using GrimSpace.World.StarSystem.Areas;
using GrimSpace.World.StarSystem.Contracts;
using GrimSpace.World.StarSystem.Encounter;
using GrimSpace.World.StarSystem.Contracts.Objectives;
using GrimSpace.World.StarSystem.Generation;
using GrimSpace.World.StarSystem.Landmarks;
using GrimSpace.World.StarSystem.Poi;
using GrimSpace.World.StarSystem.Poi.Concrete;
using GrimSpace.World.StarSystem.Resources;
using GrimSpace.Tests.World.StarSystem;
using GrimSpace.Tests.World.StarSystem.Poi;

namespace GrimSpace.Tests.World.StarSystem.Contracts;

[StarSystemTestSuite]
public sealed class ContractFactoryDeliveryTests(StarMapFixture maps)
{
	[Fact]
	public void Create_Delivery_WithOverride_RegistersPendingContract()
	{
		var map = maps.Fresh(42);
		var plan = map.Blueprint.SupplyPlan;
		var dropoffPoiId = plan.ExitPoiId;
		var dropoffFacilityId = Facility.ScopedId(plan.ExitPoiId, Wormhole.TravelFacilitySlug);
		var dropoffOperatorName = MapFacilityOperators.TravelOperatorName(map);
		var args = new DeliveryCreateArgs(
			plan.StoragePoiId,
			EDangerLevel.VeryLow,
			ContractNarrative.ForDelivery("Test Delivery", "Pick up here.", "Drop off there."),
			DropoffPoiId: dropoffPoiId,
			DropoffFacilityId: dropoffFacilityId,
			DropoffOperatorName: dropoffOperatorName);

		var contract = ContractFactory.Create(map, EContractKind.Delivery, args);

		Assert.True(map.ContractRegistry.IsPending(contract.Id));
		var objective = Assert.IsType<DeliveryObjective>(contract.Objective);
		Assert.Equal(dropoffPoiId, objective.TurnInPoiId);
		Assert.Equal(dropoffFacilityId, objective.TurnInFacilityId);
		Assert.Equal(dropoffOperatorName, objective.TurnInOperatorName);
		Assert.Equal(plan.StoragePoiId, contract.IssuerPoiId);
		Assert.Equal("Drop off there.", contract.Narrative.TurnInDialog);
		Assert.Equal(
			ContractRewardCalculator.Roll(map.Seed, contract.Id, EContractKind.Delivery, args.Danger),
			contract.Terms);
	}

	[Fact]
	public void Create_Delivery_WithMismatchedArgs_Throws()
	{
		var map = maps.Fresh(42);
		var huntArgs = new HuntCreateArgs(
			map.Blueprint.SupplyPlan.AdministrativePoiId,
			new AreaPickerArgs(MapLandmarkQueries.AllIds(map)),
			EDangerLevel.VeryLow,
			ContractNarrative.ForHunt("Hunt"));

		Assert.Throws<ArgumentException>(() =>
			ContractFactory.Create(map, EContractKind.Delivery, huntArgs));
	}

	[Fact]
	public void BeatBDropoff_UsesWormholeTravelLoungeDropoff()
	{
		var map = maps.Fresh(42);
		var plan = map.Blueprint.SupplyPlan;
		var (_, dropoffPoiId, dropoffFacilityId, dropoffOperatorName) = TutorialBeatContracts.BeatBDropoff(map);

		Assert.Equal(plan.ExitPoiId, dropoffPoiId);
		Assert.Equal(
			Facility.ScopedId(plan.ExitPoiId, Wormhole.TravelFacilitySlug),
			dropoffFacilityId);
		Assert.Equal(MapFacilityOperators.TravelOperatorName(map), dropoffOperatorName);
	}

	[Fact]
	public void Display_Delivery_IdentifiesDestinationFacility()
	{
		var map = maps.Fresh(42);
		var plan = map.Blueprint.SupplyPlan;
		var dropoff = map.PointsOfInterest.First(poi => poi.Id == plan.TradeHubPoiId);
		var facility = dropoff.Facilities.First(f => f.DisplayName == "Dockyard");
		var contract = ContractFactory.Build(
			map,
			"delivery-display",
			EContractKind.Delivery,
			new DeliveryCreateArgs(
				plan.StoragePoiId,
				EDangerLevel.VeryLow,
				ContractNarrative.ForDelivery("Delivery", "Cargo.", "Received."),
				DropoffPoiId: dropoff.Id,
				DropoffFacilityId: facility.Id,
				DropoffOperatorName: facility.Operators[0].Name));

		Assert.Contains($"Dockyard at {dropoff.DisplayName}", ContractDisplay.SearchArea(contract, map));
		Assert.Contains($"Dockyard at {dropoff.DisplayName}", ContractDisplay.ObjectivePreview(contract, map));
		Assert.Contains($"Dockyard at {dropoff.DisplayName}", ContractDisplay.DetailsBody(contract, map));
	}
}
