using GrimSpace.Tutorials;
using GrimSpace.World.StarSystem;
using GrimSpace.World.StarSystem.Contracts;
using GrimSpace.World.StarSystem.Contracts.Objectives;
using GrimSpace.World.StarSystem.Generation;
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
			new ContractTerms(ResourceBundle.Of(ResourceId.Credits, 50)),
			ContractNarrative.ForDelivery("Test Delivery", "Pick up here.", "Drop off there."),
			IsStoryObjective: false,
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
	}

	[Fact]
	public void Create_Delivery_WithMismatchedArgs_Throws()
	{
		var map = maps.Fresh(42);

		Assert.Throws<ArgumentException>(() =>
			ContractFactory.Create(map, EContractKind.Delivery, TutorialBeatContracts.CreateBeatAHuntArgs(map)));
	}

	[Fact]
	public void CreateBeatBDeliveryArgs_UsesWormholeTravelLoungeDropoff()
	{
		var map = maps.Fresh(42);
		var plan = map.Blueprint.SupplyPlan;
		var args = TutorialBeatContracts.CreateBeatBDeliveryArgs(map);

		Assert.Equal(plan.StoragePoiId, args.IssuerPoiId);
		Assert.Equal(plan.ExitPoiId, args.DropoffPoiId);
		Assert.Equal(
			Facility.ScopedId(plan.ExitPoiId, Wormhole.TravelFacilitySlug),
			args.DropoffFacilityId);
		Assert.Equal(MapFacilityOperators.TravelOperatorName(map), args.DropoffOperatorName);
	}
}
