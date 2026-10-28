using GrimSpace.Battle.Objectives;
using GrimSpace.Core.Actions;
using GrimSpace.Core.Engine;
using GrimSpace.Math.Grid;
using GrimSpace.Pilots;
using GrimSpace.Run;
using GrimSpace.World.StarSystem;
using GrimSpace.World.StarSystem.Actions;
using GrimSpace.World.StarSystem.Contact;
using GrimSpace.World.StarSystem.Effects;
using GrimSpace.World.StarSystem.Runtime;
using GrimSpace.World.StarSystem.Economy;
using GrimSpace.World.StarSystem.Resources;
using GrimSpace.World.StarSystem.PilotCapabilities;
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
		var destinationBefore = orchestrator.Map.StateOf(RunState.PlayerFleetUnitId).Journey().Destination;
		orchestrator.PlayerAgent!.TryEnqueue([new EngageAction(RunState.PlayerFleetUnitId)]);
		orchestrator.AdvanceClock();

		var player = orchestrator.Map.StateOf(RunState.PlayerFleetUnitId);
		Assert.NotEqual(destinationBefore, player.AtRest().Position);
	}

	private StarSystemOrchestrator CreateAwaitingDecisionScenario(bool inTransit = false)
	{
		var map = maps.Fresh(42);
		StarSystemTestHarness.AddPlayerFleet(map, RunState.PlayerFleetUnitId);
		var pirateId = "pirate-a";
		map.FleetRegistry.Add(StarSystemTestHarness.CreatePirateFleet(
			pirateId,
			new Coord(4, 0, 0),
			GrimSpace.World.Factions.EFaction.Pirates));
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
	public void FuelCost_CountsAllOwnedPilotsUsingActualLevels()
	{
		using var orchestrator = CreateAwaitingDecisionScenario();
		var map = orchestrator.Map;
		map.PilotRegistry = new RunPilotRegistry();
		var active = Pilot.CreateForShip("active", new Random(1)).WithAssignment("active", true);
		var inactive = Pilot.CreateForShip("inactive", new Random(2)).WithExperience(100);
		var unassigned = Pilot.CreateForShip("unassigned", new Random(3))
			.WithExperience(250)
			.WithAssignment(null, false);
		map.PilotRegistry.Register(active);
		map.PilotRegistry.Register(inactive);
		map.PilotRegistry.Register(unassigned);

		Assert.Equal(1, inactive.ClaimedLevel);
		Assert.Equal(48, FleeDef.FuelCost(map));
	}

	[Theory]
	[InlineData(9, false)]
	[InlineData(10, true)]
	[InlineData(11, true)]
	public void Commit_RequiresFullFuelPayment(int availableFuel, bool canFlee)
	{
		using var orchestrator = CreateAwaitingDecisionScenario();
		var map = orchestrator.Map;
		map.PilotRegistry = new RunPilotRegistry();
		map.PilotRegistry.Register(Pilot.CreateForShip("reserve", new Random(1)));
		Assert.True(map.PlayerResources.TryApply(ResourceBundle.Of(
			ResourceId.Fuel,
			availableFuel - map.PlayerResources.GetBalance(ResourceId.Fuel))));
		using var engine = new Engine<StarMap, ActorRuntime>(map, new ActorRuntimes<ActorRuntime>());
		var action = new FleeAction(RunState.PlayerFleetUnitId);
		var counterpartyEngagement = map.StateOf("pirate-a").CurrentEngagement;

		Assert.Equal(canFlee, FleeDef.Instance.IsLegal(action, map, new ActorRuntime()));
		Assert.True(EngageDef.Instance.IsLegal(
			new EngageAction(RunState.PlayerFleetUnitId), map, new ActorRuntime()));
		var history = engine.Commit(action);

		Assert.Equal(availableFuel - (canFlee ? 10 : 0), map.PlayerResources.GetBalance(ResourceId.Fuel));
		Assert.Equal(
			canFlee ? EEngagementPhase.None : EEngagementPhase.AwaitingDecision,
			EngagementAssertions.Phase(map.StateOf(RunState.PlayerFleetUnitId)));
		if (canFlee)
			Assert.Null(map.StateOf("pirate-a").CurrentEngagement);
		else
			Assert.Same(counterpartyEngagement, map.StateOf("pirate-a").CurrentEngagement);
		var transactions = history.OfType<Record<Transaction>>()
			.Where(record => record.Value.Source == TransactionSource.FleeFuel)
			.ToArray();
		if (canFlee)
			Assert.Equal(ResourceBundle.Of(ResourceId.Fuel, -10), Assert.Single(transactions).Value.Change);
		else
			Assert.Empty(transactions);
	}

	[Theory]
	[InlineData(55, false)]
	[InlineData(56, true)]
	public void PendingEngagement_ProjectsCostAndAffordability(int availableFuel, bool canFlee)
	{
		using var orchestrator = CreateAwaitingDecisionScenario();
		var map = orchestrator.Map;
		map.PilotRegistry = new RunPilotRegistry();
		map.PilotRegistry.Register(Pilot.CreateForShip("a", new Random(1)).WithExperience(250));
		map.PilotRegistry.Register(Pilot.CreateForShip("b", new Random(2)).WithExperience(250));
		map.PilotRegistry.Register(Pilot.CreateForShip("c", new Random(3)).WithExperience(0));
		Assert.True(map.PlayerResources.TryApply(ResourceBundle.Of(
			ResourceId.Fuel,
			availableFuel - map.PlayerResources.GetBalance(ResourceId.Fuel))));

		Assert.True(EngagementQueries.TryGetPendingPlayerEngagement(map, RunState.PlayerFleetUnitId, out var pending));
		Assert.Equal(56, pending.FleeFuelCost);
		Assert.Equal(availableFuel, pending.AvailableFuel);
		Assert.Equal(canFlee, pending.CanFlee);
	}

	[Fact]
	public void Simulation_FleeDoesNotChargeLiveResourcesOrClearLiveEngagement()
	{
		using var orchestrator = CreateAwaitingDecisionScenario();
		var map = orchestrator.Map;
		map.PilotRegistry = new RunPilotRegistry();
		map.PilotRegistry.Register(Pilot.CreateForShip("reserve", new Random(1)));
		var fuelBefore = map.PlayerResources.GetBalance(ResourceId.Fuel);
		var sim = orchestrator.CreateSimulation();

		Assert.True(sim.TryEnqueue(new FleeAction(RunState.PlayerFleetUnitId)));

		Assert.Equal(fuelBefore, map.PlayerResources.GetBalance(ResourceId.Fuel));
		Assert.Equal(EEngagementPhase.AwaitingDecision,
			EngagementAssertions.Phase(map.StateOf(RunState.PlayerFleetUnitId)));
	}

	[Fact]
	public void FuelCost_WithoutRoster_IsZero()
	{
		using var orchestrator = CreateAwaitingDecisionScenario();
		orchestrator.Map.PilotRegistry = null;
		Assert.Equal(0, FleeDef.FuelCost(orchestrator.Map));
	}

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
			GrimSpace.World.Factions.EFaction.Pirates));
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
			GrimSpace.World.Factions.EFaction.Pirates));
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

	[Fact]
	public void Commit_AppliesSalvagePipelineToLootAward()
	{
		var map = maps.Fresh(43);
		StarSystemTestHarness.AddPlayerFleet(map, RunState.PlayerFleetUnitId);
		var pirateId = "pirate-loot";
		map.FleetRegistry.Add(StarSystemTestHarness.CreatePirateFleet(
			pirateId,
			new Coord(4, 0, 0),
			GrimSpace.World.Factions.EFaction.Pirates));
		new CommitEngagementEffect(RunState.PlayerFleetUnitId, pirateId)
			.Apply(map, new ActorRuntime(), RunState.PlayerFleetUnitId);
		var outcome = new BattleOutcome(
			map.StateOf(RunState.PlayerFleetUnitId).CurrentEngagement!.Id,
			EBattleResult.Win,
			[]);
		var rolls = new[]
		{
			new LootRoll(
				"miner-1",
				GrimSpace.Units.Enums.EType.RepurposedMiner,
				ResourceBundle.Of(ResourceId.ScrapAlloy, 4)),
			new LootRoll(
				"miner-2",
				GrimSpace.Units.Enums.EType.RepurposedMiner,
				ResourceBundle.Create(
					(ResourceId.ScrapAlloy, 4),
					(ResourceId.IndustrialCore, 1))),
		};
		var runtimes = new ActorRuntimes<ActorRuntime>();
		runtimes.For(RunState.PlayerFleetUnitId).ReplaceActionEffects(
			StarMapPilotCapabilityPipelineFactory.Build(
				[new PilotCapabilityGrant(
					EPilotCapabilityId.ScrapRewardBonus,
					"pilot-1",
					new PilotCapabilitySource(
						EPilotCapabilitySourceKind.Skill,
						EPilotSkillId.DronesYearnForMines.ToString()))]));
		var engine = new Engine<StarMap, ActorRuntime>(map, runtimes);

		engine.Commit(new ResolveEngagementAction(
			RunState.PlayerFleetUnitId,
			outcome,
			rolls,
			ResourceBundle.Create(
				(ResourceId.ScrapAlloy, 8),
				(ResourceId.IndustrialCore, 1))));

		Assert.Equal(
			StrategicEconomyConfig.StartingScrapAlloy + 10,
			map.PlayerResources.GetBalance(ResourceId.ScrapAlloy));
		Assert.Equal(1, map.PlayerResources.GetBalance(ResourceId.IndustrialCore));
	}
}
