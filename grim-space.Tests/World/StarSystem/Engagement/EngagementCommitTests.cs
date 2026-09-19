using GrimSpace.Battle.Objectives;
using GrimSpace.Core.Actions;
using GrimSpace.Math.Grid;
using GrimSpace.Run;
using GrimSpace.World.StarSystem;
using GrimSpace.World.StarSystem.Actions;
using GrimSpace.World.StarSystem.Contact;
using GrimSpace.World.StarSystem.Effects;
using GrimSpace.World.StarSystem.Narrative;
using GrimSpace.World.StarSystem.Runtime;
using GrimSpace.Tests.World.StarSystem;
using GrimSpace.Tests.World.StarSystem.Traffic;
using RunState = GrimSpace.Run.State;

namespace GrimSpace.Tests.World.StarSystem.Engagement;

public sealed class EngagementCommitTests(StarMapFixture maps)
{
	[Fact]
	public void CommitEngagementEffect_EmitsOneEngagementCommittedRecord()
	{
		var map = maps.Fresh(42);
		StarSystemTestHarness.AddPlayerFleet(map, RunState.PlayerFleetUnitId);
		var pirateId = "pirate-a";
		AddPirate(map, pirateId);
		PrepareAwaitingDecision(map, pirateId);

		var records = new CommitEngagementEffect(RunState.PlayerFleetUnitId, pirateId)
			.Apply(map, new ActorRuntime(), RunState.PlayerFleetUnitId);

		var record = Assert.IsType<Record<EngagementCommitted>>(Assert.Single(records));
		Assert.Equal(map.StateOf(RunState.PlayerFleetUnitId).CurrentEngagement!.Id, record.Value.EngagementId);
		Assert.Equal(
			new[] { pirateId, RunState.PlayerFleetUnitId },
			record.Value.ParticipantFleetIds);
	}

	[Fact]
	public void SimulationPeek_DoesNotNotifyRunEngagementSubscribers()
	{
		using var run = RunState.CreateNewRun(42);
		AddPirate(run.StarSystem.Map, "pirate-a");
		PrepareAwaitingDecision(run.StarSystem.Map, "pirate-a");

		var notifications = 0;
		using var subscription = run.StarSystem.Subscribe<Record<EngagementCommitted>>(_ => notifications++);

		var sim = run.StarSystem.CreateSimulation();
		Assert.True(sim.TryEnqueue(new EngageAction(RunState.PlayerFleetUnitId)));
		_ = sim.Peek(new EngageAction(RunState.PlayerFleetUnitId));

		Assert.Equal(0, notifications);
	}

	[Fact]
	public void CommittedEngagement_CreatesOneActiveBattle()
	{
		using var run = RunState.CreateNewRun(42);
		var battleReady = 0;
		run.BattleReady += () => battleReady++;

		CommitPlayerEngagement(run);

		Assert.NotNull(run.ActiveBattle);
		Assert.Equal(1, battleReady);
	}

	[Fact]
	public void DuplicateEngagementFacts_DoNotCreateDuplicateBattles()
	{
		using var run = RunState.CreateNewRun(42);
		var battleReady = 0;
		run.BattleReady += () => battleReady++;
		var fact = CommitPlayerEngagement(run);
		var firstBattle = run.ActiveBattle;

		run.ReceiveEngagementFact(new Record<EngagementCommitted>(fact));

		Assert.Same(firstBattle, run.ActiveBattle);
		Assert.Equal(1, battleReady);
	}

	[Fact]
	public void NonPlayerEngagement_DoesNotLaunchPlayerBattle()
	{
		using var run = RunState.CreateNewRun(42);
		var hunterId = "hunter-a";
		var preyId = "prey-b";
		var fact = new EngagementCommitted(
			"npc-engagement",
			hunterId,
			[hunterId, preyId]);

		var battleReady = 0;
		run.BattleReady += () => battleReady++;
		run.ReceiveEngagementFact(new Record<EngagementCommitted>(fact));

		Assert.Null(run.ActiveBattle);
		Assert.Equal(0, battleReady);
	}

	[Fact]
	public void CreateActiveBattleOrchestrator_SubscribesBeforeReturn()
	{
		using var run = RunState.CreateNewRun(42);
		CommitPlayerEngagement(run);

		using var orchestrator = run.CreateActiveBattleOrchestrator();
		orchestrator.ForceOutcome(EBattleResult.Win);

		Assert.Null(run.ActiveBattle);
	}

	[Fact]
	public void CommittedOutcome_ResolvesStarSystemWithoutSession()
	{
		using var run = RunState.CreateNewRun(42);
		var pirateId = "pirate-a";
		CommitPlayerEngagement(run);
		using var orchestrator = run.CreateActiveBattleOrchestrator();
		orchestrator.ForceOutcome(EBattleResult.Win);

		Assert.Null(run.ActiveBattle);
		Assert.False(run.StarSystem.Map.FleetRegistry.Contains(pirateId));
	}

	[Fact]
	public void RegeneratingStarSystem_DisposesOldEngagementSubscription()
	{
		using var run = RunState.CreateNewRun(42);
		var oldSystem = run.StarSystem;
		var notifications = 0;
		using var probe = oldSystem.Subscribe<Record<EngagementCommitted>>(_ => notifications++);

		run.RegenerateMap(99);

		Assert.NotSame(oldSystem, run.StarSystem);
		CommitPlayerEngagement(run);
		Assert.Equal(0, notifications);
	}

	[Fact]
	public void ResourceTransitions_SurviveMapRegenerationOnSameRun()
	{
		using var run = RunState.CreateNewRun(42);
		var inbox = run.Transitions;
		run.RegenerateMap(77);

		Assert.Same(inbox, run.Transitions);
		Assert.NotNull(run.StarSystem);
	}

	[Fact]
	public void DevDuelPath_DoesNotRequireActiveBattle()
	{
		using var run = RunState.CreateNewRun(42);
		Assert.Null(run.ActiveBattle);
		Assert.Throws<InvalidOperationException>(() => run.CreateActiveBattleOrchestrator());
	}

	private static EngagementCommitted CommitPlayerEngagement(RunState run)
	{
		DismissOpeningNarrative(run);
		AddPirate(run.StarSystem.Map, "pirate-a");
		PrepareAwaitingDecision(run.StarSystem.Map, "pirate-a");
		run.StarSystem.PlayerAgent!.TryEnqueue([new EngageAction(RunState.PlayerFleetUnitId)]);
		run.StarSystem.AdvanceClock();
		Assert.True(EngagementQueries.TryGetCommittedPlayerEngagement(
			run.StarSystem.Map,
			RunState.PlayerFleetUnitId,
			out var committed));
		return new EngagementCommitted(
			committed.EngagementId,
			committed.InitiatorUnitId,
			committed.ParticipantUnitIds);
	}

	private static void DismissOpeningNarrative(RunState run)
	{
		if (run.StarSystem.Map.ActiveNarrativeId is null)
			return;

		run.StarSystem.PlayerAgent!.TryEnqueue([
			new CompleteNarrativeAction(RunState.PlayerFleetUnitId, MapNarratives.OpeningId),
		]);
		run.StarSystem.AdvanceClock();
	}

	private static void PrepareAwaitingDecision(StarMap map, string pirateId)
	{
		new SetEngagementIntentEffect(RunState.PlayerFleetUnitId, pirateId)
			.Apply(map, new ActorRuntime(), RunState.PlayerFleetUnitId);
		new ReachContactEffect(RunState.PlayerFleetUnitId, pirateId)
			.Apply(map, new ActorRuntime(), RunState.PlayerFleetUnitId);
	}

	private static void AddPirate(StarMap map, string pirateId, Coord? position = null) =>
		map.FleetRegistry.Add(StarSystemTestHarness.CreatePirateFleet(
			pirateId,
			position ?? new Coord(4, 0, 0),
			GrimSpace.World.Factions.EFaction.Pirates,
			new GrimSpace.World.StarSystem.Encounter.CombatProfile(
				GrimSpace.World.StarSystem.Encounter.EDangerLevel.VeryLow,
				1)));
}
