using GrimSpace.Core.Ids;
using GrimSpace.Run;
using GrimSpace.World.StarSystem;
using GrimSpace.World.StarSystem.Actions;
using GrimSpace.World.StarSystem.Agents;
using GrimSpace.World.StarSystem.Contracts;
using GrimSpace.World.StarSystem.Contracts.Objectives;
using GrimSpace.World.StarSystem.Generation;
using GrimSpace.World.StarSystem.Poi;
using GrimSpace.World.StarSystem.Poi.Concrete;
using GrimSpace.World.StarSystem.Resources;
using GrimSpace.Tests.World.StarSystem.Poi;
using GrimSpace.Tests.World.StarSystem.Traffic;
using GrimSpace.Tests.World.StarSystem;

namespace GrimSpace.Tests.World.StarSystem.Contracts;

[StarSystemTestSuite]
public sealed class DeclineContractActionTests(StarMapFixture maps)
{
	[Fact]
	public void TryEnqueue_FailsForStoryObjectiveContract()
	{
		var orchestrator = CreateOrchestrator();
		var agent = orchestrator.PlayerAgent!;
		var contractId = orchestrator.Map.ContractRegistry.Pending.First().Id;

		Assert.False(agent.TryEnqueue([ContractActionTestContext.Decline(orchestrator.Map, State.PlayerFleetUnitId, contractId)]));
	}

	[Fact]
	public void TryEnqueue_SucceedsForOfferedContract()
	{
		var orchestrator = CreateOrchestrator();
		var agent = orchestrator.PlayerAgent!;
		var contractId = RegisterDeclineableOffer(orchestrator.Map);

		Assert.True(agent.TryEnqueue([ContractActionTestContext.Decline(orchestrator.Map, State.PlayerFleetUnitId, contractId)]));
	}

	[Fact]
	public void Commit_PersistsRejection()
	{
		var orchestrator = CreateOrchestrator();
		var agent = orchestrator.PlayerAgent!;
		var contractId = RegisterDeclineableOffer(orchestrator.Map);

		Assert.True(agent.TryEnqueue([ContractActionTestContext.Decline(orchestrator.Map, State.PlayerFleetUnitId, contractId)]));
		orchestrator.AdvanceClock();

		Assert.False(orchestrator.Map.ContractRegistry.IsPending(contractId));
		Assert.True(orchestrator.Map.ContractRegistry.IsRejected(contractId));
	}

	[Fact]
	public void AdvanceClock_CommitsOnEnqueue()
	{
		var orchestrator = CreateOrchestrator();
		var contractId = RegisterDeclineableOffer(orchestrator.Map);

		Assert.True(orchestrator.PlayerAgent!.TryEnqueue(
			[ContractActionTestContext.Decline(orchestrator.Map, State.PlayerFleetUnitId, contractId)]));
		orchestrator.AdvanceClock();

		Assert.False(orchestrator.Map.ContractRegistry.IsPending(contractId));
		Assert.True(orchestrator.Map.ContractRegistry.IsRejected(contractId));
	}

	[Fact]
	public void TryEnqueue_FailsForAlreadyRejectedContract()
	{
		var orchestrator = CreateOrchestrator();
		var contractId = RegisterDeclineableOffer(orchestrator.Map);
		orchestrator.Map.ContractRegistry.Activate(new ContractState(
			contractId,
			EContractStatus.Rejected,
			null,
			null));
		var agent = orchestrator.PlayerAgent!;

		Assert.False(agent.TryEnqueue([ContractActionTestContext.Decline(orchestrator.Map, State.PlayerFleetUnitId, contractId)]));
	}

	[Fact]
	public void AdvanceClock_ThenAdvanceTick_DoesNotDoubleCommit()
	{
		var orchestrator = CreateOrchestrator();
		var contractId = RegisterDeclineableOffer(orchestrator.Map);

		Assert.True(orchestrator.PlayerAgent!.TryEnqueue(
			[ContractActionTestContext.Decline(orchestrator.Map, State.PlayerFleetUnitId, contractId)]));
		orchestrator.AdvanceClock();

		Assert.True(orchestrator.Map.ContractRegistry.IsRejected(contractId));

		orchestrator.AdvanceTick();

		Assert.True(orchestrator.Map.ContractRegistry.IsRejected(contractId));
	}

	private static string RegisterDeclineableOffer(StarMap map)
	{
		var starter = map.ContractRegistry.Pending.First();
		var hunt = (HuntObjective)starter.Objective;
		var contractId = TypedIdGenerator.NextId("contract");
		Assert.True(map.ContractRegistry.TryAdd(new Contract(
			contractId,
			hunt,
			starter.IssuerFaction,
			starter.IssuerPoiId,
			starter.Terms,
			ContractNarrative.ForHunt("Optional Hunt"),
			ContractFactory.IsHuntObjectiveMet)));
		return contractId;
	}

	private StarSystemOrchestrator CreateOrchestrator() =>
		StarSystemTestHarness.CreatePlayerOrchestrator(maps, State.PlayerFleetUnitId, 42);
}
