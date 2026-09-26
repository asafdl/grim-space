using GrimSpace.Battle.Objectives;
using GrimSpace.Core.Engine;
using GrimSpace.Math.Grid;
using GrimSpace.Run;
using GrimSpace.World.StarSystem;
using GrimSpace.World.StarSystem.Actions;
using GrimSpace.World.StarSystem.Effects;
using GrimSpace.World.StarSystem.Runtime;
using GrimSpace.World.StarSystem.Units;
using GrimSpace.Tests.World.StarSystem.Traffic;
using RunState = GrimSpace.Run.State;
using GrimSpace.Tests.World.StarSystem;

namespace GrimSpace.Tests.World.StarSystem.Engagement;

[StarSystemTestSuite]
public sealed class EngageActionTests(StarMapFixture maps)
{
	[Fact]
	public void Commit_CreatesSymmetricEngaged()
	{
		var orchestrator = CreateAwaitingDecisionScenario();
		orchestrator.PlayerAgent!.TryEnqueue([new EngageAction(RunState.PlayerFleetUnitId)]);
		orchestrator.AdvanceClock();

		var player = orchestrator.Map.StateOf(RunState.PlayerFleetUnitId);
		var pirateId = EngagementAssertions.EngagedCounterparty(player)!;
		var pirate = orchestrator.Map.StateOf(pirateId);

		Assert.Equal(EEngagementPhase.Engaged, EngagementAssertions.Phase(player));
		Assert.Equal(EEngagementPhase.Engaged, EngagementAssertions.Phase(pirate));
		Assert.Same(player.CurrentEngagement, pirate.CurrentEngagement);
		Assert.Contains(pirateId, EngagementAssertions.Participants(player));
		Assert.Contains(RunState.PlayerFleetUnitId, EngagementAssertions.Participants(pirate));
		Assert.Null(EngagementAssertions.Hunting(player));
		Assert.Null(EngagementAssertions.HuntedBy(pirate));
	}

	[Fact]
	public void IsLegal_RejectsNonAwaitingDecision()
	{
		var map = maps.Fresh(42);
		StarSystemTestHarness.AddPlayerFleet(map, RunState.PlayerFleetUnitId);
		var orchestrator = StarSystemTestHarness.CreatePlayerOrchestrator(maps, RunState.PlayerFleetUnitId, 42, map: map);
		var sim = orchestrator.CreateSimulation();

		Assert.False(sim.TryEnqueue(new EngageAction(RunState.PlayerFleetUnitId)));
	}

	[Fact]
	public void Commit_MaterializesInTransitInitiator()
	{
		var orchestrator = CreateAwaitingDecisionScenario(inTransit: true);
		var destinationBefore = orchestrator.Map.StateOf(RunState.PlayerFleetUnitId).Journey.Destination;
		orchestrator.PlayerAgent!.TryEnqueue([new EngageAction(RunState.PlayerFleetUnitId)]);
		orchestrator.AdvanceClock();

		var player = orchestrator.Map.StateOf(RunState.PlayerFleetUnitId);
		Assert.Equal(EPhase.Docked, player.Phase);
		Assert.Equal("", player.DockedAtDockId);
		Assert.NotEqual(destinationBefore, player.IdleCoord);
	}

	private StarSystemOrchestrator CreateAwaitingDecisionScenario(bool inTransit = false)
	{
		var map = maps.Fresh(42);
		StarSystemTestHarness.AddPlayerFleet(map, RunState.PlayerFleetUnitId);
		var pirateId = "pirate-a";
		map.FleetRegistry.Add(StarSystemTestHarness.CreatePirateFleet(
			pirateId,
			new Coord(4, 0, 0),
			GrimSpace.World.Factions.EFaction.Pirates,
			new GrimSpace.World.StarSystem.Encounter.CombatProfile()));
		new SetEngagementIntentEffect(RunState.PlayerFleetUnitId, pirateId)
			.Apply(map, new ActorRuntime(), RunState.PlayerFleetUnitId);
		new ReachContactEffect(RunState.PlayerFleetUnitId, pirateId)
			.Apply(map, new ActorRuntime(), RunState.PlayerFleetUnitId);

		if (inTransit)
		{
			var player = map.FleetRegistry.FleetOf(RunState.PlayerFleetUnitId);
			var runtime = new ActorRuntime();
			var path = GrimSpace.World.StarSystem.Pathfinding.TransitPath.FromPoints(
				[new Coord(0, 0, 0), new Coord(20, 0, 20)],
				[1.0, 1.0]);
			foreach (var effect in GrimSpace.World.StarSystem.Effects.MovementEffects.BeginJourney(
				RunState.PlayerFleetUnitId,
				runtime,
				map,
				new Coord(0, 0, 0),
				new Coord(20, 0, 20),
				path))
				effect.Apply(map, runtime, RunState.PlayerFleetUnitId);
		}

		return StarSystemTestHarness.CreatePlayerOrchestrator(maps, RunState.PlayerFleetUnitId, 42, map: map);
	}
}

[StarSystemTestSuite]
public sealed class FleeActionTests(StarMapFixture maps)
{
	[Fact]
	public void Commit_ResolvesWithoutOutcome()
	{
		var orchestrator = CreateAwaitingDecisionScenario();
		const string pirateId = "pirate-a";
		orchestrator.PlayerAgent!.TryEnqueue([new FleeAction(RunState.PlayerFleetUnitId)]);
		orchestrator.AdvanceClock();

		var player = orchestrator.Map.StateOf(RunState.PlayerFleetUnitId);
		Assert.Equal(EEngagementPhase.None, EngagementAssertions.Phase(player));
		Assert.Null(EngagementAssertions.Hunting(player));
		Assert.Empty(EngagementAssertions.Participants(player));
		Assert.Null(EngagementAssertions.HuntedBy(orchestrator.Map.StateOf(pirateId)));
		Assert.Equal(EEngagementPhase.None, EngagementAssertions.Phase(orchestrator.Map.StateOf(pirateId)));
		Assert.True(orchestrator.CanAdvance);
	}

	[Fact]
	public void IsLegal_RejectsNonAwaitingDecision()
	{
		var map = maps.Fresh(42);
		StarSystemTestHarness.AddPlayerFleet(map, RunState.PlayerFleetUnitId);
		var orchestrator = StarSystemTestHarness.CreatePlayerOrchestrator(maps, RunState.PlayerFleetUnitId, 42, map: map);
		var sim = orchestrator.CreateSimulation();

		Assert.False(sim.TryEnqueue(new FleeAction(RunState.PlayerFleetUnitId)));
	}

	private StarSystemOrchestrator CreateAwaitingDecisionScenario()
	{
		var map = maps.Fresh(42);
		StarSystemTestHarness.AddPlayerFleet(map, RunState.PlayerFleetUnitId);
		var pirateId = "pirate-a";
		map.FleetRegistry.Add(StarSystemTestHarness.CreatePirateFleet(
			pirateId,
			new Coord(4, 0, 0),
			GrimSpace.World.Factions.EFaction.Pirates,
			new GrimSpace.World.StarSystem.Encounter.CombatProfile()));
		new SetEngagementIntentEffect(RunState.PlayerFleetUnitId, pirateId)
			.Apply(map, new ActorRuntime(), RunState.PlayerFleetUnitId);
		new ReachContactEffect(RunState.PlayerFleetUnitId, pirateId)
			.Apply(map, new ActorRuntime(), RunState.PlayerFleetUnitId);

		return StarSystemTestHarness.CreatePlayerOrchestrator(maps, RunState.PlayerFleetUnitId, 42, map: map);
	}
}

[StarSystemTestSuite]
public sealed class ResolveEngagementActionTests(StarMapFixture maps)
{
	[Fact]
	public void Commit_StoresEachParticipantState()
	{
		var map = maps.Fresh(42);
		StarSystemTestHarness.AddPlayerFleet(map, RunState.PlayerFleetUnitId);
		var pirateId = "pirate-a";
		map.FleetRegistry.Add(StarSystemTestHarness.CreatePirateFleet(
			pirateId,
			new Coord(4, 0, 0),
			GrimSpace.World.Factions.EFaction.Pirates,
			new GrimSpace.World.StarSystem.Encounter.CombatProfile()));
		new CommitEngagementEffect(RunState.PlayerFleetUnitId, pirateId)
			.Apply(map, new ActorRuntime(), RunState.PlayerFleetUnitId);
		var handoffs = map.FleetRegistry.All
			.SelectMany(fleet => fleet.Members.Select(member => OutcomeTestKit.Handoff(
				member.Id,
				OutcomeTestKit.ChassisFromShipId(member.Id),
				fleet.State.Id == pirateId ? 0 : 1)))
			.ToArray();
		var outcome = new BattleOutcome(
			map.StateOf(RunState.PlayerFleetUnitId).CurrentEngagement!.Id,
			EBattleResult.Win,
			handoffs);

		var engine = new Engine<StarMap, ActorRuntime>(map, new ActorRuntimes<ActorRuntime>());
		var loot = LootCatalog.For(outcome);
		engine.Commit([new ResolveEngagementAction(
			RunState.PlayerFleetUnitId,
			outcome,
			loot.Rolls,
			loot.Total)]);

		Assert.Equal(EEngagementPhase.None, EngagementAssertions.Phase(map.StateOf(RunState.PlayerFleetUnitId)));
		Assert.DoesNotContain(pirateId, map.FleetRegistry.Ids);
		Assert.Contains(RunState.PlayerFleetUnitId, map.FleetRegistry.Ids);
	}
}
