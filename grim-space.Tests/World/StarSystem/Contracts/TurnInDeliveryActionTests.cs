using GrimSpace.Core.Engine;
using GrimSpace.Run;
using GrimSpace.Tutorials;
using GrimSpace.World.StarSystem;
using GrimSpace.World.StarSystem.Actions;
using GrimSpace.World.StarSystem.Contracts;
using GrimSpace.World.StarSystem.Contracts.Objectives;
using GrimSpace.World.StarSystem.Effects;
using GrimSpace.World.StarSystem.Encounter;
using GrimSpace.World.StarSystem.Ids;
using GrimSpace.World.StarSystem.Poi;
using GrimSpace.World.StarSystem.Resources;
using GrimSpace.World.StarSystem.Runtime;
using GrimSpace.Tests.World.StarSystem.Traffic;

namespace GrimSpace.Tests.World.StarSystem.Contracts;

[StarSystemTestSuite]
public sealed class CompleteDeliveryFacilityLegActionTests(StarMapFixture maps)
{
	[Fact]
	public void AcceptDelivery_GrantsTurnInOverlayOnWormholeOperator()
	{
		var (engine, unitId, contractId, delivery) = CreateDeliveryEngine();
		engine.Commit(ContractActionTestContext.AcceptDelivery(engine.World, unitId, contractId));

		var turnInPoi = engine.World.GetPointOfInterest(delivery.TurnInPoiId);
		Assert.True(turnInPoi.OperatorTemporaryRoles.TryGetRole(
			delivery.TurnInFacilityId,
			delivery.TurnInOperatorName,
			out var role));
		Assert.Equal(EFacilityOperatorRole.DeliveryTurnIn, role);
		Assert.Equal(
			EFacilityOperatorRole.DeliveryTurnIn,
			turnInPoi.ResolveInteractionRole(
				delivery.TurnInFacilityId,
				delivery.TurnInOperatorName,
				EFacilityOperatorRole.Dialog));
	}

	[Fact]
	public void TurnInThenComplete_RevokesOverlayAndGrantsPaymentOnce()
	{
		var (engine, unitId, contractId, delivery) = CreateDeliveryEngine();
		engine.Commit(ContractActionTestContext.AcceptDelivery(engine.World, unitId, contractId));
		engine.Commit(ContractActionTestContext.TurnInDelivery(engine.World, unitId, contractId));
		var delivered = Assert.IsType<DeliveryContractState>(
			engine.World.ContractRegistry.TryGetState(contractId, out var deliveredState)
				? deliveredState
				: null);
		Assert.Null(delivered.Progress.DeadlineTick);
		var completion = Assert.IsType<CompleteContractAction>(
			Assert.Single(ContractReevaluation.ReevaluateFor(
				engine.World,
				engine.ActorRuntimes.For(unitId),
				unitId,
				EContractKind.Delivery)));
		engine.Commit(completion);

		Assert.True(engine.World.ContractRegistry.IsCompleted(contractId));
		Assert.False(engine.World.GetPointOfInterest(delivery.TurnInPoiId).OperatorTemporaryRoles.TryGetRole(
			delivery.TurnInFacilityId,
			delivery.TurnInOperatorName,
			out _));
		Assert.Equal(
			TutorialBeatContracts.BeatBDeliveryRewardCredits,
			engine.World.PlayerResources.GetBalance(ResourceId.Credits));

		var creditsAfterFirst = engine.World.PlayerResources.GetBalance(ResourceId.Credits);
		engine.Commit(completion);
		Assert.Equal(creditsAfterFirst, engine.World.PlayerResources.GetBalance(ResourceId.Credits));
	}

	[Fact]
	public void TurnIn_WithWrongOperator_IsIllegal()
	{
		var (engine, unitId, contractId, _) = CreateDeliveryEngine();
		engine.Commit(ContractActionTestContext.AcceptDelivery(engine.World, unitId, contractId));

		var wrongTurnIn = new CompleteDeliveryFacilityLegAction(
			unitId,
			ContractActionTestContext.AdministrativePoiId,
			ContractActionTestContext.ManagementFacilityId,
			"Wrong Operator",
			contractId,
			0);
		var sim = engine.CreateSimulation();

		Assert.False(sim.TryEnqueue(wrongTurnIn));
	}

	[Fact]
	public void CompleteSpaceMeetingLeg_ResumesSimulation()
	{
		var map = maps.Fresh(42);
		StarSystemTestHarness.AddPlayerFleet(map, State.PlayerFleetUnitId);
		var contract = ContractFactory.Create(
			map,
			EContractKind.Delivery,
			new DeliveryCreateArgs(
				map.Blueprint.SupplyPlan.StoragePoiId,
				EDangerLevel.Moderate,
				ContractNarrative.ForDelivery("Delivery", "Cargo.", "Received."),
				Generation: new DeliveryGenerationConfig(2, spaceMeetingChance: 1.0)));
		var meeting = Assert.IsType<SpaceMeetingDeliveryLeg>(
			((DeliveryObjective)contract.Objective).Route.Legs[0]);
		var engine = new Engine<StarMap, ActorRuntime>(
			map,
			new ActorRuntimes<ActorRuntime>());
		engine.Commit(ContractActionTestContext.AcceptDelivery(
			map,
			State.PlayerFleetUnitId,
			contract.Id));
		new PlayerInputEffect(true).Apply(
			map,
			engine.ActorRuntimes.For(State.PlayerFleetUnitId),
			State.PlayerFleetUnitId);

		engine.Commit([new CompleteDeliveryFacilityLegAction(
			State.PlayerFleetUnitId,
			"",
			"",
			"",
			contract.Id,
			0,
			meeting.MeetingId)]);

		Assert.False(map.WaitingForPlayerInput);
	}

	[Fact]
	public void CompleteLeg_ReplacesDeadlineForNextLeg()
	{
		var map = maps.Fresh(42);
		StarSystemTestHarness.AddPlayerFleet(map, State.PlayerFleetUnitId);
		var contract = ContractFactory.Create(
			map,
			EContractKind.Delivery,
			new DeliveryCreateArgs(
				map.Blueprint.SupplyPlan.StoragePoiId,
				EDangerLevel.Moderate,
				ContractNarrative.ForDelivery("Delivery", "Cargo.", "Received."),
				Generation: new DeliveryGenerationConfig(
					facilityLegCount: 2,
					spaceMeetingChance: 0)));
		var delivery = Assert.IsType<DeliveryObjective>(contract.Objective);
		var firstLeg = Assert.IsType<FacilityDeliveryLeg>(delivery.Route.Legs[0]);
		var engine = new Engine<StarMap, ActorRuntime>(
			map,
			new ActorRuntimes<ActorRuntime>());
		engine.Commit(ContractActionTestContext.AcceptDelivery(
			map,
			State.PlayerFleetUnitId,
			contract.Id));
		var firstState = Assert.IsType<DeliveryContractState>(
			map.ContractRegistry.TryGetState(contract.Id, out var state) ? state : null);
		var firstDeadline = firstState.Progress.DeadlineTick!.Value;
		map.Timeline.Clock.Set(firstDeadline - 1);

		engine.Commit(new CompleteDeliveryFacilityLegAction(
			State.PlayerFleetUnitId,
			firstLeg.PoiId,
			firstLeg.FacilityId,
			firstLeg.OperatorName,
			contract.Id,
			0));

		var nextState = Assert.IsType<DeliveryContractState>(
			map.ContractRegistry.TryGetState(contract.Id, out state) ? state : null);
		Assert.Equal(1, nextState.Progress.CurrentLegIndex);
		Assert.True(nextState.Progress.DeadlineTick > firstDeadline);
		map.Timeline.Clock.Set(firstDeadline + 1);
		Assert.False(FailDeliveryDeadlineDef.Instance.IsLegal(
			new FailDeliveryDeadlineAction(StarSystemActorIds.Contracts, contract.Id),
			map,
			engine.ActorRuntimes.For(StarSystemActorIds.Contracts)));
	}

	private (Engine<StarMap, ActorRuntime> Engine, string UnitId, string ContractId, DeliveryObjective Delivery)
		CreateDeliveryEngine(int seed = 42)
	{
		var map = maps.Fresh(seed);
		StarSystemTestHarness.AddPlayerFleet(map, State.PlayerFleetUnitId);
		var contractId = TutorialBeatContracts.OfferBeatB(map);
		var contract = map.ContractRegistry.All.First(c => c.Id == contractId);
		var delivery = (DeliveryObjective)contract.Objective;

		var runtimes = new ActorRuntimes<ActorRuntime>();
		runtimes.For(State.PlayerFleetUnitId);
		var engine = new Engine<StarMap, ActorRuntime>(map, runtimes);
		return (engine, State.PlayerFleetUnitId, contractId, delivery);
	}
}
