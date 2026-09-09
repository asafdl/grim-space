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

public sealed class EngageActionTests(DevStarMapFixture maps)
{
	[Fact]
	public void Commit_CreatesSymmetricEngaged()
	{
		var orchestrator = CreateAwaitingDecisionScenario();
		orchestrator.PlayerAgent!.TryEnqueue([new EngageAction(RunState.PlayerFleetUnitId)]);
		orchestrator.AdvanceClock();

		var player = orchestrator.Map.StateOf(RunState.PlayerFleetUnitId);
		var pirateId = player.EngagedWithUnitIds.Single();
		var pirate = orchestrator.Map.StateOf(pirateId);

		Assert.Equal(EEngagementPhase.Engaged, player.EngagementPhase);
		Assert.Equal(EEngagementPhase.Engaged, pirate.EngagementPhase);
		Assert.Contains(pirateId, player.EngagedWithUnitIds);
		Assert.Contains(RunState.PlayerFleetUnitId, pirate.EngagedWithUnitIds);
		Assert.Null(player.EngagementTargetUnitId);
		Assert.Null(pirate.HuntedByUnitId);
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
		map.UnitRegistry.Add(Factory.CreatePirateFleet(
			pirateId,
			new Coord(4, 0, 0),
			GrimSpace.World.Factions.EFaction.Pirates,
			new GrimSpace.World.StarSystem.Encounter.CombatProfile(
				GrimSpace.World.StarSystem.Encounter.EDangerLevel.VeryLow,
				1)));
		new SetEngagementIntentEffect(RunState.PlayerFleetUnitId, pirateId)
			.Apply(map, new ActorRuntime(), RunState.PlayerFleetUnitId);
		new ReachContactEffect(RunState.PlayerFleetUnitId, pirateId)
			.Apply(map, new ActorRuntime(), RunState.PlayerFleetUnitId);

		if (inTransit)
		{
			var player = map.UnitRegistry.UnitOf(RunState.PlayerFleetUnitId);
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

public sealed class FleeActionTests(DevStarMapFixture maps)
{
	[Fact]
	public void Commit_ResolvesWithoutOutcome()
	{
		var orchestrator = CreateAwaitingDecisionScenario();
		const string pirateId = "pirate-a";
		orchestrator.PlayerAgent!.TryEnqueue([new FleeAction(RunState.PlayerFleetUnitId)]);
		orchestrator.AdvanceClock();

		var player = orchestrator.Map.StateOf(RunState.PlayerFleetUnitId);
		Assert.Equal(EEngagementPhase.Resolved, player.EngagementPhase);
		Assert.Null(player.ResolvedEngagementOutcome);
		Assert.Null(player.EngagementTargetUnitId);
		Assert.Empty(player.EngagedWithUnitIds);
		Assert.Null(orchestrator.Map.StateOf(pirateId).HuntedByUnitId);
		Assert.Equal(EEngagementPhase.None, orchestrator.Map.StateOf(pirateId).EngagementPhase);
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
		map.UnitRegistry.Add(Factory.CreatePirateFleet(
			pirateId,
			new Coord(4, 0, 0),
			GrimSpace.World.Factions.EFaction.Pirates,
			new GrimSpace.World.StarSystem.Encounter.CombatProfile(
				GrimSpace.World.StarSystem.Encounter.EDangerLevel.VeryLow,
				1)));
		new SetEngagementIntentEffect(RunState.PlayerFleetUnitId, pirateId)
			.Apply(map, new ActorRuntime(), RunState.PlayerFleetUnitId);
		new ReachContactEffect(RunState.PlayerFleetUnitId, pirateId)
			.Apply(map, new ActorRuntime(), RunState.PlayerFleetUnitId);

		return StarSystemTestHarness.CreatePlayerOrchestrator(maps, RunState.PlayerFleetUnitId, 42, map: map);
	}
}

public sealed class ResolveEngagementActionTests(DevStarMapFixture maps)
{
	[Fact]
	public void Commit_StoresMatchingOutcomeOnBothSides()
	{
		var map = maps.Fresh(42);
		StarSystemTestHarness.AddPlayerFleet(map, RunState.PlayerFleetUnitId);
		var pirateId = "pirate-a";
		map.UnitRegistry.Add(Factory.CreatePirateFleet(
			pirateId,
			new Coord(4, 0, 0),
			GrimSpace.World.Factions.EFaction.Pirates,
			new GrimSpace.World.StarSystem.Encounter.CombatProfile(
				GrimSpace.World.StarSystem.Encounter.EDangerLevel.VeryLow,
				1)));
		new CommitEngagementEffect(RunState.PlayerFleetUnitId, pirateId)
			.Apply(map, new ActorRuntime(), RunState.PlayerFleetUnitId);

		var engine = new Engine<StarMap, ActorRuntime>(map, new ActorRuntimes<ActorRuntime>());
		engine.Commit([new ResolveEngagementAction(RunState.PlayerFleetUnitId, BattleOutcome.Win)]);

		Assert.Equal(EEngagementPhase.Resolved, map.StateOf(RunState.PlayerFleetUnitId).EngagementPhase);
		Assert.Equal(EEngagementPhase.Resolved, map.StateOf(pirateId).EngagementPhase);
		Assert.Equal(BattleOutcome.Win, map.StateOf(RunState.PlayerFleetUnitId).ResolvedEngagementOutcome);
		Assert.Equal(BattleOutcome.Win, map.StateOf(pirateId).ResolvedEngagementOutcome);
		Assert.Contains(pirateId, map.UnitRegistry.Ids);
		Assert.Contains(RunState.PlayerFleetUnitId, map.UnitRegistry.Ids);
	}
}
