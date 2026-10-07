using GrimSpace.Core.Engine;
using GrimSpace.Tutorials;
using GrimSpace.Math.Grid;
using GrimSpace.World.StarSystem;
using GrimSpace.World.StarSystem.Actions;
using GrimSpace.World.StarSystem.Contracts;
using GrimSpace.World.StarSystem.Contracts.Objectives;
using GrimSpace.World.StarSystem.Encounter;
using GrimSpace.World.StarSystem.Ids;
using GrimSpace.World.StarSystem.Poi;
using GrimSpace.World.StarSystem.Poi.Concrete;
using GrimSpace.World.StarSystem.Runtime;
using GrimSpace.Tests.World.StarSystem;
using GrimSpace.Tests.World.StarSystem.Poi;

namespace GrimSpace.Tests.World.StarSystem.Contracts;

[StarSystemTestSuite]
public sealed class DeliveryAcceptanceTests(StarMapFixture maps)
{
	[Fact]
	public void RollTimedLeg_IsDeterministicForSeedAndLegIndex()
	{
		const int seed = 42;
		const string contractId = "delivery-timed-roll";

		var first = DeliveryContractState.RollTimedLeg(
			seed,
			contractId,
			legIndex: 0,
			DeliveryGenerationConfig.Default);
		var second = DeliveryContractState.RollTimedLeg(
			seed,
			contractId,
			legIndex: 0,
			DeliveryGenerationConfig.Default);
		Assert.Equal(first, second);
		Assert.False(DeliveryContractState.RollTimedLeg(
			seed,
			contractId,
			legIndex: 0,
			timedLegChance: 0));
	}

	[Fact]
	public void RollInterception_IsDeterministicForSeedAndDanger()
	{
		const int seed = 42;
		const string contractId = "delivery-roll";

		var first = DeliveryContractState.RollInterception(
			seed,
			contractId,
			EDangerLevel.Moderate,
			DeliveryGenerationConfig.Default);
		var second = DeliveryContractState.RollInterception(
			seed,
			contractId,
			EDangerLevel.Moderate,
			DeliveryGenerationConfig.Default);
		Assert.Equal(first, second);
		Assert.False(DeliveryContractState.RollInterception(
			seed,
			contractId,
			EDangerLevel.VeryLow,
			DeliveryGenerationConfig.Default));
	}

	[Fact]
	public void EstimateLegTravelTicks_UsesManhattanDistanceFromLegOrigin()
	{
		var map = maps.Fresh(42);
		var plan = map.Blueprint.SupplyPlan;
		var pickup = map.GetPointOfInterest(plan.StoragePoiId).PlacedCenter;
		var dropoff = map.GetPointOfInterest(plan.ExitPoiId).PlacedCenter;
		var route = new DeliveryRoute(
		[
			new FacilityDeliveryLeg(
				plan.StoragePoiId,
				Facility.ScopedId(plan.StoragePoiId, StorageFacility.WarehouseFacilitySlug),
				"pickup"),
			new FacilityDeliveryLeg(
				plan.ExitPoiId,
				Facility.ScopedId(plan.ExitPoiId, Wormhole.TravelFacilitySlug),
				"dropoff"),
		]);

		var travelTicks = DeliveryContractState.EstimateLegTravelTicks(
			map,
			route,
			legIndex: 1,
			pickup);
		var expectedDistance = pickup.ManhattanDistanceTo(dropoff);
		Assert.Equal(expectedDistance / 6d, travelTicks);
	}

	[Fact]
	public void AcceptDelivery_SchedulesContractTimelineAndSetsProgress()
	{
		var map = maps.Fresh(42);
		var plan = map.Blueprint.SupplyPlan;
		var contract = ContractFactory.Build(
			map,
			"delivery-accept-timeline",
			EContractKind.Delivery,
			new DeliveryCreateArgs(
				plan.StoragePoiId,
				EDangerLevel.Moderate,
				ContractNarrative.ForDelivery("Delivery", "Pickup.", "Dropoff."),
				DropoffPoiId: plan.ExitPoiId,
				DropoffFacilityId: Facility.ScopedId(plan.ExitPoiId, Wormhole.TravelFacilitySlug),
				DropoffOperatorName: MapFacilityOperators.TravelOperatorName(map),
				Generation: new DeliveryGenerationConfig(timedLegChance: 1.0)));
		map.ContractRegistry.TryAdd(contract);
		var unit = map.FleetRegistry.All.First();
		var runtimes = new ActorRuntimes<ActorRuntime>();
		runtimes.For(unit.State.Id);
		runtimes.For(StarSystemActorIds.Contracts);
		var engine = new Engine<StarMap, ActorRuntime>(map, runtimes);
		var acceptedAtTick = engine.Tick;

		engine.Commit(ContractActionTestContext.AcceptDelivery(map, unit.State.Id, contract.Id));

		Assert.True(engine.World.ContractRegistry.TryGetState(contract.Id, out var state));
		var delivery = Assert.IsType<DeliveryContractState>(state);
		Assert.Equal(acceptedAtTick, delivery.Progress.ActivationTick);
		var objective = Assert.IsType<DeliveryObjective>(contract.Objective);
		var origin = map.GetPointOfInterest(contract.IssuerPoiId!).PlacedCenter;
		Assert.Equal(
			DeliveryContractState.ResolveDeadlineTickForLeg(
				map,
				contract.Id,
				objective,
				legIndex: 0,
				origin,
				acceptedAtTick,
				DeliveryContractState.TimedLegChanceFor(contract, objective.Config)),
			delivery.Progress.DeadlineTick);
		Assert.Equal(
			delivery.Progress.InterceptionState == EDeliveryInterceptionState.Pending,
			engine.World.Timeline.ContainsPending(action =>
				action is AttemptDeliveryInterceptionAction attempt
				&& attempt.ContractId == contract.Id
				&& attempt.ActorId == StarSystemActorIds.Contracts));
		Assert.True(engine.World.Timeline.ContainsPending(action =>
			action is FailDeliveryDeadlineAction fail
			&& fail.ContractId == contract.Id
			&& fail.ActorId == StarSystemActorIds.Contracts));
	}

	[Fact]
	public void AcceptStoryDelivery_SkipsDeadlineAndInterceptionTimeline()
	{
		var map = maps.Fresh(42);
		TutorialBeatContracts.OfferBeatB(map);
		var contract = map.ContractRegistry.Pending.Single();
		Assert.True(contract.IsStoryObjective);
		var unit = map.FleetRegistry.All.First();
		var runtimes = new ActorRuntimes<ActorRuntime>();
		runtimes.For(unit.State.Id);
		runtimes.For(StarSystemActorIds.Contracts);
		var engine = new Engine<StarMap, ActorRuntime>(map, runtimes);

		engine.Commit(ContractActionTestContext.AcceptDelivery(map, unit.State.Id, contract.Id));

		var delivery = Assert.IsType<DeliveryContractState>(
			engine.World.ContractRegistry.TryGetState(contract.Id, out var state) ? state : null);
		Assert.Null(delivery.Progress.DeadlineTick);
		Assert.Equal(EDeliveryInterceptionState.None, delivery.Progress.InterceptionState);
		Assert.False(engine.World.Timeline.ContainsPending(action =>
			action is AttemptDeliveryInterceptionAction));
		Assert.False(engine.World.Timeline.ContainsPending(action =>
			action is FailDeliveryDeadlineAction));
	}

	[Fact]
	public void AcceptHunt_DoesNotScheduleDeliveryTimeline()
	{
		var (engine, unitId, contractId) = CreateHuntEngine();

		engine.Commit(ContractActionTestContext.Accept(engine.World, unitId, contractId));

		Assert.False(engine.World.Timeline.ContainsPending(action =>
			action is FailDeliveryDeadlineAction));
	}

	[Fact]
	public void DeadlineAction_FailsDeliveryWithDeadlineReason()
	{
		var map = maps.Fresh(42);
		var plan = map.Blueprint.SupplyPlan;
		var contract = ContractFactory.Build(
			map,
			"delivery-deadline",
			EContractKind.Delivery,
			new DeliveryCreateArgs(
				plan.StoragePoiId,
				EDangerLevel.Moderate,
				ContractNarrative.ForDelivery("Delivery", "Pickup.", "Dropoff."),
				DropoffPoiId: plan.ExitPoiId,
				DropoffFacilityId: Facility.ScopedId(plan.ExitPoiId, Wormhole.TravelFacilitySlug),
				DropoffOperatorName: MapFacilityOperators.TravelOperatorName(map),
				Generation: new DeliveryGenerationConfig(timedLegChance: 1.0)));
		map.ContractRegistry.TryAdd(contract);
		var unit = map.FleetRegistry.All.First();
		var runtimes = new ActorRuntimes<ActorRuntime>();
		runtimes.For(unit.State.Id);
		runtimes.For(StarSystemActorIds.Contracts);
		var engine = new Engine<StarMap, ActorRuntime>(map, runtimes);
		engine.Commit(ContractActionTestContext.AcceptDelivery(map, unit.State.Id, contract.Id));
		var delivery = Assert.IsType<DeliveryContractState>(
			map.ContractRegistry.TryGetState(contract.Id, out var state) ? state : null);
		Assert.NotNull(delivery.Progress.DeadlineTick);
		map.Timeline.Clock.Set(delivery.Progress.DeadlineTick!.Value + 1);

		engine.Commit(new FailDeliveryDeadlineAction(StarSystemActorIds.Contracts, contract.Id));

		delivery = Assert.IsType<DeliveryContractState>(
			map.ContractRegistry.TryGetState(contract.Id, out state) ? state : null);
		Assert.Equal(EContractStatus.Failed, delivery.Status);
		Assert.Equal(EDeliveryFailureReason.Deadline, delivery.Progress.FailureReason);
	}

	private (Engine<StarMap, ActorRuntime> engine, string unitId, string contractId) CreateHuntEngine(
		int seed = 42)
	{
		var map = maps.FreshWithBeatAHunt(seed);
		var unitId = map.FleetRegistry.All.First().State.Id;
		var runtimes = new ActorRuntimes<ActorRuntime>();
		runtimes.For(unitId);
		var engine = new Engine<StarMap, ActorRuntime>(map, runtimes);
		var contractId = map.ContractRegistry.Pending.First().Id;
		return (engine, unitId, contractId);
	}
}
