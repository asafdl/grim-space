using GrimSpace.Core.Actions;
using GrimSpace.Core.Engine;
using GrimSpace.Math.Grid;
using GrimSpace.Run;
using GrimSpace.World.Factions;
using GrimSpace.World.StarSystem;
using GrimSpace.World.StarSystem.Actions;
using GrimSpace.World.StarSystem.Contact;
using GrimSpace.World.StarSystem.Encounter;
using GrimSpace.World.StarSystem.Pathfinding;
using GrimSpace.World.StarSystem.Runtime;
using GrimSpace.World.StarSystem.Units;
using GrimSpace.Tests.World.StarSystem.Traffic;
using RunState = GrimSpace.Run.State;
using GrimSpace.Tests.World.StarSystem;

namespace GrimSpace.Tests.World.StarSystem.Engagement;

[StarSystemTestSuite]
public sealed class PursueContactActionTests(StarMapFixture maps)
{
	[Fact]
	public void Preview_DoesNotMutateLiveMap()
	{
		var (orchestrator, playerId, pirateId) = CreateScenario();
		var destination = orchestrator.CommittedPositionOf(pirateId);

		Assert.True(orchestrator.PlayerAgent!.TryEnqueue([
			CreatePursueAction(orchestrator, playerId, pirateId, destination)]));

		Assert.Null(EngagementAssertions.Hunting(orchestrator.Map.StateOf(playerId)));
		Assert.Null(EngagementAssertions.HuntedBy(orchestrator.Map.StateOf(pirateId)));
		Assert.Equal(EPhase.Docked, orchestrator.Map.StateOf(playerId).Phase);
	}

	[Fact]
	public void Commit_StoresBidirectionalHuntLinkAndStartsJourney()
	{
		var (orchestrator, playerId, pirateId) = CreateScenario();
		var destination = orchestrator.CommittedPositionOf(pirateId);
		orchestrator.PlayerAgent!.TryEnqueue([CreatePursueAction(orchestrator, playerId, pirateId, destination)]);
		orchestrator.AdvanceTick();

		Assert.Equal(pirateId, EngagementAssertions.Hunting(orchestrator.Map.StateOf(playerId)));
		Assert.Equal(EEngagementPhase.Pursuing, EngagementAssertions.Phase(orchestrator.Map.StateOf(playerId)));
		Assert.Equal(playerId, EngagementAssertions.HuntedBy(orchestrator.Map.StateOf(pirateId)));
		Assert.Equal(EPhase.InTransit, orchestrator.Map.StateOf(playerId).Phase);
		Assert.Equal(destination, orchestrator.Map.StateOf(playerId).Journey.Destination);
		Assert.True(orchestrator.Map.StateOf(playerId).TravelTarget.MatchesFleet(pirateId));
		Assert.Equal(
			EContactIntent.Engagement,
			orchestrator.Map.StateOf(playerId).TravelTarget.ContactIntent);
	}

	[Fact]
	public void Commit_ReplaceTarget_UpdatesBothSides()
	{
		var (orchestrator, playerId, firstPirateId) = CreateScenario();
		var secondPirateId = AddPirate(orchestrator.Map, "pirate-b", new Coord(40, 0, 40));
		QueuePursue(orchestrator, playerId, firstPirateId);
		orchestrator.AdvanceTick();
		QueuePursue(orchestrator, playerId, secondPirateId);
		orchestrator.AdvanceTick();

		Assert.Equal(secondPirateId, EngagementAssertions.Hunting(orchestrator.Map.StateOf(playerId)));
		Assert.Null(EngagementAssertions.HuntedBy(orchestrator.Map.StateOf(firstPirateId)));
		Assert.Equal(playerId, EngagementAssertions.HuntedBy(orchestrator.Map.StateOf(secondPirateId)));
	}

	[Fact]
	public void IsLegal_RejectsSelfTarget()
	{
		var (orchestrator, playerId, _) = CreateScenario();
		var sim = orchestrator.CreateSimulation();
		var destination = orchestrator.CommittedPositionOf(playerId);

		Assert.False(sim.TryEnqueue(CreatePursueAction(orchestrator, playerId, playerId, destination)));
	}

	[Fact]
	public void IsLegal_AllowsTrafficFleetTarget()
	{
		var map = maps.Fresh(42);
		StarSystemTestHarness.AddPlayerFleet(map, RunState.PlayerFleetUnitId);
		var trafficUnit = map.FleetRegistry.All.First(unit => unit.State.ChoreDockIds.Count > 0);
		var orchestrator = StarSystemTestHarness.CreatePlayerOrchestrator(maps, RunState.PlayerFleetUnitId, 42, map: map);
		var sim = orchestrator.CreateSimulation();
		var destination = orchestrator.CommittedPositionOf(trafficUnit.State.Id);

		Assert.True(sim.TryEnqueue(CreatePursueAction(
			orchestrator,
			RunState.PlayerFleetUnitId,
			trafficUnit.State.Id,
			destination)));
	}

	[Fact]
	public void IsLegal_RejectsUnsupportedFleetContactIntent()
	{
		var (orchestrator, playerId, pirateId) = CreateScenario();
		var destination = orchestrator.CommittedPositionOf(pirateId);
		var action = CreatePursueAction(orchestrator, playerId, pirateId, destination)
			with { Intent = EContactIntent.WreckInvestigation };

		Assert.False(orchestrator.CreateSimulation().TryEnqueue(action));
	}

	[Fact]
	public void Move_ClearsHuntLink()
	{
		var (orchestrator, playerId, pirateId) = CreateScenario();
		QueuePursue(orchestrator, playerId, pirateId);
		orchestrator.AdvanceTick();

		var destination = new Coord(5, 0, 5);
		orchestrator.PlayerAgent!.TryQueueMove(destination);
		orchestrator.AdvanceTick();

		Assert.Null(EngagementAssertions.Hunting(orchestrator.Map.StateOf(playerId)));
		Assert.Null(EngagementAssertions.HuntedBy(orchestrator.Map.StateOf(pirateId)));
		Assert.False(orchestrator.Map.StateOf(playerId).TravelTarget.IsActive);
	}

	[Fact]
	public void Commit_PirateEngagementPursuitUsesOneAndHalfSpeed()
	{
		var map = maps.Fresh(42);
		StarSystemTestHarness.AddPlayerFleet(map, RunState.PlayerFleetUnitId);
		var pirateId = AddPirate(map, "pirate-a", new Coord(20, 0, 20));
		var pirate = map.FleetRegistry.FleetOf(pirateId);
		var destination = map.DocksById[
			map.StateOf(RunState.PlayerFleetUnitId).DockedAtDockId].Position;
		var path = TransitPath.FromPoints(
			[pirate.State.IdleCoord, destination],
			[1.0, 1.0]);
		var actorRuntimes = new ActorRuntimes<ActorRuntime>();
		var runtime = actorRuntimes.For(pirateId);
		actorRuntimes.For(RunState.PlayerFleetUnitId);
		var engine = new Engine<StarMap, ActorRuntime>(map, actorRuntimes);
		var pursuitChanges = new List<FleetPursuitChanged>();
		using var subscription = engine.Subscribe<Record<FleetPursuitChanged>>(
			record => pursuitChanges.Add(record.Value));

		var action = new PursueContactAction(
			pirateId,
			new FleetContactTarget(RunState.PlayerFleetUnitId),
			destination,
			path,
			EContactIntent.Engagement);
		engine.Commit(action);
		engine.Commit(action);

		var committedPath = Assert.IsType<TransitPath>(runtime.CachedPath);
		Assert.Equal(
			path.TicksRequired(pirate.State.SpeedPerTick * 1.5),
			committedPath.TicksRequired(pirate.State.SpeedPerTick),
			10);
		Assert.Equal(
			pirate.State.SpeedPerTick * PathfindingCell.RouteSpeedCeiling * 1.5,
			EngagementQueries.MaximumTravelSpeed(pirate.State),
			10);
		Assert.Equal(
			new FleetPursuitChanged(pirateId, RunState.PlayerFleetUnitId, true),
			Assert.Single(pursuitChanges));
	}

	[Fact]
	public void Commit_PlayerEngagementPursuitKeepsBaseSpeed()
	{
		var map = maps.Fresh(42);
		StarSystemTestHarness.AddPlayerFleet(map, RunState.PlayerFleetUnitId);
		var pirateId = AddPirate(map, "pirate-a", new Coord(20, 0, 20));
		var player = map.FleetRegistry.FleetOf(RunState.PlayerFleetUnitId);
		var origin = map.DocksById[player.State.DockedAtDockId].Position;
		var destination = map.StateOf(pirateId).IdleCoord;
		var path = TransitPath.FromPoints([origin, destination], [1.0, 1.0]);
		var actorRuntimes = new ActorRuntimes<ActorRuntime>();
		var runtime = actorRuntimes.For(RunState.PlayerFleetUnitId);
		actorRuntimes.For(pirateId);
		var engine = new Engine<StarMap, ActorRuntime>(map, actorRuntimes);

		engine.Commit(new PursueContactAction(
			RunState.PlayerFleetUnitId,
			new FleetContactTarget(pirateId),
			destination,
			path,
			EContactIntent.Engagement));

		Assert.Same(path, runtime.CachedPath);
		Assert.Equal(
			player.State.SpeedPerTick * PathfindingCell.RouteSpeedCeiling,
			EngagementQueries.MaximumTravelSpeed(player.State),
			10);
	}

	private (StarSystemOrchestrator orchestrator, string playerId, string pirateId) CreateScenario()
	{
		var map = maps.Fresh(42);
		StarSystemTestHarness.AddPlayerFleet(map, RunState.PlayerFleetUnitId);
		var pirateId = AddPirate(map, "pirate-a", new Coord(20, 0, 20));
		var orchestrator = StarSystemTestHarness.CreatePlayerOrchestrator(maps, 
			RunState.PlayerFleetUnitId,
			42,
			map: map);
		return (orchestrator, RunState.PlayerFleetUnitId, pirateId);
	}

	private static PursueContactAction CreatePursueAction(
		StarSystemOrchestrator orchestrator,
		string playerId,
		string targetId,
		Coord destination)
	{
		var origin = orchestrator.CommittedPositionOf(playerId);
		var path = TransitPath.FromPoints([origin, destination], [1.0, 1.0]);
		return new PursueContactAction(
			playerId,
			new FleetContactTarget(targetId),
			destination,
			path,
			EContactIntent.Engagement);
	}

	private static void QueuePursue(StarSystemOrchestrator orchestrator, string playerId, string targetId)
	{
		var destination = orchestrator.CommittedPositionOf(targetId);
		orchestrator.PlayerAgent!.TryEnqueue([CreatePursueAction(orchestrator, playerId, targetId, destination)]);
	}

	private static string AddPirate(StarMap map, string id, Coord coord)
	{
		map.FleetRegistry.Add(StarSystemTestHarness.CreatePirateFleet(
			id,
			coord,
			EFaction.Pirates));
		return id;
	}
}
