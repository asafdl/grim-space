using GrimSpace.Core.Engine;
using GrimSpace.Run;
using GrimSpace.Tutorials;
using GrimSpace.World.StarSystem;
using GrimSpace.World.StarSystem.Actions;
using GrimSpace.World.StarSystem.Agents;
using GrimSpace.World.StarSystem.Contracts;
using GrimSpace.World.StarSystem.Contracts.Generation;
using GrimSpace.World.StarSystem.Contracts.Objectives;
using GrimSpace.World.StarSystem.Ids;
using GrimSpace.World.StarSystem.Runtime;
using GrimSpace.Tests.World.StarSystem;
using GrimSpace.Tests.World.StarSystem.Traffic;

namespace GrimSpace.Tests.World.StarSystem.Contracts.Generation;

[StarSystemTestSuite]
public sealed class ContractBoardExecutionAgentTests(StarMapFixture maps)
{
	[Fact]
	public void Plan_WhenGenerationDisabled_WithDueExpiry_ExpiresOnly()
	{
		var map = maps.Fresh(42);
		var due = ContractFactory.Build(
			map,
			"due-generated",
			EContractKind.Hunt,
			TutorialBeatContracts.CreateBeatAHuntArgs(map) with { IsStoryObjective = false });
		map.ContractRegistry.TryAdd(due, expiresAtTick: 5);
		var action = PlanAtTick(map, tick: 5, generationEnabled: false);

		Assert.NotNull(action);
		Assert.Equal(5, action.Tick);
		Assert.Empty(action.Additions);
	}

	[Fact]
	public void Plan_OnCadence_FillsGeneratedBoard()
	{
		var map = maps.Fresh(42);
		var config = new ContractBoardConfig
		{
			CadenceTicks = 5,
			Placement = new ContractPlacementConfig { TargetGeneratedCount = 3 },
		};
		var action = PlanAtTick(map, tick: 5, generationEnabled: true, config);

		Assert.NotNull(action);
		Assert.Equal(3, action.Additions.Count);
		Assert.All(action.Additions, addition => Assert.False(addition.Contract.IsStoryObjective));
		Assert.All(
			action.Additions,
			addition => Assert.Equal(5 + ContractBoardConfig.DefaultTtlTicks, addition.ExpiresAtTick));
	}

	[Fact]
	public void Plan_OffCadence_PublishesWithNoAdditions()
	{
		var map = maps.Fresh(42);
		var action = PlanAtTick(map, tick: 4, generationEnabled: true);

		Assert.NotNull(action);
		Assert.Empty(action.Additions);
	}

	[Fact]
	public void Plan_WhenOfferExpired_PublishesOffCadenceWithNoAdditions()
	{
		var map = maps.Fresh(42);
		var due = ContractFactory.Build(
			map,
			"due-generated",
			EContractKind.Hunt,
			TutorialBeatContracts.CreateBeatAHuntArgs(map) with { IsStoryObjective = false });
		map.ContractRegistry.TryAdd(due, expiresAtTick: 7);

		var action = PlanAtTick(map, tick: 7, generationEnabled: false);
		Assert.NotNull(action);
		Assert.Empty(action.Additions);
	}

	[Fact]
	public void Plan_RespectsMaxPendingCap()
	{
		var map = maps.Fresh(42);
		map.ContractRegistry.MaxPending = 2;
		var config = new ContractBoardConfig
		{
			CadenceTicks = 1,
			Placement = new ContractPlacementConfig { TargetGeneratedCount = 3 },
		};
		var action = PlanAtTick(map, tick: 1, generationEnabled: true, config);

		Assert.NotNull(action);
		Assert.Equal(2, action.Additions.Count);
	}

	[Fact]
	public void Plan_SameInputs_ProduceDeterministicContractIds()
	{
		var map = maps.Fresh(99);
		var first = PlanAtTick(map, tick: 10, generationEnabled: true);
		var second = PlanAtTick(map, tick: 10, generationEnabled: true);

		Assert.NotNull(first);
		Assert.NotNull(second);
		Assert.Equal(
			first.Additions.Select(addition => addition.Contract.Id),
			second.Additions.Select(addition => addition.Contract.Id));
	}

	[Fact]
	public void Plan_WreckageOnlyWeight_CanProduceWreckageContracts()
	{
		var map = maps.Fresh(55);
		var config = new ContractBoardConfig
		{
			CadenceTicks = 1,
			Placement = new ContractPlacementConfig
			{
				TargetGeneratedCount = 6,
				HuntKindWeight = 0f,
				DeliveryKindWeight = 0f,
				WreckageKindWeight = 1f,
			},
		};
		var action = PlanAtTick(map, tick: 1, generationEnabled: true, config);
		Assert.NotNull(action);
		Assert.Contains(
			action.Additions,
			addition => addition.Contract.Objective is WreckageObjective);
	}

	[Fact]
	public void Commit_OnCadence_RegistersGeneratedContracts()
	{
		var map = maps.Fresh(42);
		var engine = CreateEngine(map);
		var config = new ContractBoardConfig { CadenceTicks = 1 };
		var action = PlanAtTick(map, engine.Tick, generationEnabled: true, config);
		Assert.NotNull(action);

		engine.Commit(action);

		var generated = engine.World.ContractRegistry.Pending.Where(contract => !contract.IsStoryObjective).ToList();
		Assert.Equal(3, generated.Count);
	}

	[Fact]
	public void Commit_ExpiresBeforeCadenceTopUp()
	{
		var map = maps.Fresh(42);
		map.ContractRegistry.MaxPending = 2;
		var engine = CreateEngine(map);
		var config = new ContractBoardConfig { CadenceTicks = 1, TtlTicks = 10 };
		var tick = engine.Tick;
		var due = ContractFactory.Build(
			map,
			"due-generated",
			EContractKind.Hunt,
			TutorialBeatContracts.CreateBeatAHuntArgs(map) with { IsStoryObjective = false });
		map.ContractRegistry.TryAdd(due, expiresAtTick: tick);
		var action = PlanAtTick(map, tick, generationEnabled: true, config);
		Assert.NotNull(action);

		engine.Commit(action);

		Assert.False(engine.World.ContractRegistry.IsPending("due-generated"));
	}

	private static MaintainContractBoardAction? PlanAtTick(
		StarMap map,
		int tick,
		bool generationEnabled,
		ContractBoardConfig? config = null)
	{
		var sink = new ActionBatchSink();
		map.Timeline.Clock.Set(tick);
		var agent = CreateAgent(map, () => generationEnabled, sink, config);
		agent.PlanAndPublish();
		if (!sink.TryTakeBatch(StarSystemActorIds.Contracts, out var batch) || batch.Actions.Count == 0)
			return null;

		return (MaintainContractBoardAction)batch.Actions[0];
	}

	private static ContractBoardExecutionAgent CreateAgent(
		StarMap map,
		Func<bool> generationEnabled,
		ActionBatchSink sink,
		ContractBoardConfig? config = null)
	{
		var runtimes = new ActorRuntimes<ActorRuntime>();
		runtimes.For(StarSystemActorIds.Contracts);
		var agent = new ContractBoardExecutionAgent(
			() => map,
			runtimes.For,
			generationEnabled,
			config);
		agent.Init(StarSystemActorIds.Contracts, sink.WriterFor(StarSystemActorIds.Contracts));
		agent.SetCanWork(true);
		return agent;
	}

	private static Engine<StarMap, ActorRuntime> CreateEngine(StarMap map)
	{
		var runtimes = new ActorRuntimes<ActorRuntime>();
		runtimes.For(StarSystemActorIds.Contracts);
		foreach (var id in map.FleetRegistry.Ids)
			runtimes.For(id);
		return new Engine<StarMap, ActorRuntime>(map, runtimes);
	}
}
