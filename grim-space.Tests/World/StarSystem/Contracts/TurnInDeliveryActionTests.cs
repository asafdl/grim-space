using GrimSpace.Core.Engine;
using GrimSpace.Run;
using GrimSpace.Tutorials;
using GrimSpace.World.StarSystem;
using GrimSpace.World.StarSystem.Actions;
using GrimSpace.World.StarSystem.Contracts;
using GrimSpace.World.StarSystem.Contracts.Objectives;
using GrimSpace.World.StarSystem.Poi;
using GrimSpace.World.StarSystem.Resources;
using GrimSpace.World.StarSystem.Runtime;
using GrimSpace.Tests.World.StarSystem.Traffic;

namespace GrimSpace.Tests.World.StarSystem.Contracts;

[StarSystemTestSuite]
public sealed class TurnInDeliveryActionTests(StarMapFixture maps)
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

		var wrongTurnIn = new TurnInDeliveryAction(
			unitId,
			ContractActionTestContext.AdministrativePoiId,
			ContractActionTestContext.ManagementFacilityId,
			"Wrong Operator",
			contractId);
		var sim = engine.CreateSimulation();

		Assert.False(sim.TryEnqueue(wrongTurnIn));
	}

	private (Engine<StarMap, ActorRuntime> Engine, string UnitId, string ContractId, DeliveryObjective Delivery)
		CreateDeliveryEngine(int seed = 42)
	{
		var map = maps.Fresh(seed);
		StarSystemTestHarness.AddPlayerFleet(map, State.PlayerFleetUnitId);
		var contract = ContractFactory.Create(
			map,
			EContractKind.Delivery,
			TutorialBeatContracts.CreateBeatBDeliveryArgs(map));
		var delivery = (DeliveryObjective)contract.Objective;

		var runtimes = new ActorRuntimes<ActorRuntime>();
		runtimes.For(State.PlayerFleetUnitId);
		var engine = new Engine<StarMap, ActorRuntime>(map, runtimes);
		return (engine, State.PlayerFleetUnitId, contract.Id, delivery);
	}
}
