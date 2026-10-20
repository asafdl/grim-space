using GrimSpace.Core.Engine;
using GrimSpace.Math.Grid;
using GrimSpace.World.StarSystem;
using GrimSpace.World.StarSystem.Actions;
using GrimSpace.World.StarSystem.Contact;
using GrimSpace.World.StarSystem.Effects;
using GrimSpace.World.StarSystem.Pathfinding;
using GrimSpace.World.StarSystem.Runtime;
using GrimSpace.World.StarSystem.Units;
using GrimSpace.Tests.World.StarSystem;
using GrimSpace.Tests.World.StarSystem.Traffic;
using BattleUnitType = GrimSpace.Units.Enums.EType;

namespace GrimSpace.Tests.World.StarSystem.Contact;

[StarSystemTestSuite]
public sealed class PursuitPlannerTests(StarMapFixture maps)
{
	[Fact]
	public void Plan_PublishesPursueContactForDirectiveTarget()
	{
		var map = maps.Fresh(11);
		StarSystemTestHarness.AddPlayerFleet(map, "player-pursuit");
		map.StateOf("player-pursuit").Travel =
			new FleetTravel.AtRest(new Coord(5, 0, 5));
		var pirate = StarSystemTestHarness.CreatePirateFleet(
			"pirate-pursuit",
			new Coord(20, 0, 20),
			map.ControllingFaction);
		pirate.State.PursuitDirective = new FleetPursuitDirective("delivery-1", "player-pursuit");
		map.FleetRegistry.Add(pirate);

		var (planner, _, engine) = CreatePlanner(map, pirate.State.Id);
		var action = planner.Plan(pirate.State.Id);

		Assert.NotNull(action);
		Assert.Equal(EContactIntent.Engagement, action.Intent);
		Assert.Equal("player-pursuit", Assert.IsType<FleetContactTarget>(action.Target).UnitId);
		Assert.True(PursueContactDef.Instance.IsLegal(action, map, engine.ActorRuntimes.For(pirate.State.Id)));
	}

	[Fact]
	public void Plan_SkipsReplanWhileInTransitTowardCurrentTargetPosition()
	{
		var map = maps.Fresh(11);
		StarSystemTestHarness.AddPlayerFleet(map, "player-pursuit");
		map.StateOf("player-pursuit").Travel =
			new FleetTravel.AtRest(new Coord(5, 0, 5));
		var pirate = StarSystemTestHarness.CreatePirateFleet(
			"pirate-pursuit",
			new Coord(20, 0, 20),
			map.ControllingFaction);
		pirate.State.PursuitDirective = new FleetPursuitDirective("delivery-1", "player-pursuit");
		map.FleetRegistry.Add(pirate);

		var (planner, _, engine) = CreatePlanner(map, pirate.State.Id);
		var first = planner.Plan(pirate.State.Id);
		Assert.NotNull(first);
		engine.Commit(first!);

		Assert.Null(planner.Plan(pirate.State.Id));
	}

	[Fact]
	public void Plan_ReplansWhenDirectiveTargetMoves()
	{
		var map = maps.Fresh(11);
		StarSystemTestHarness.AddPlayerFleet(map, "player-pursuit");
		var player = map.FleetRegistry.FleetOf("player-pursuit");
		player.State.Travel = new FleetTravel.AtRest(new Coord(5, 0, 5));
		var pirate = StarSystemTestHarness.CreatePirateFleet(
			"pirate-pursuit",
			new Coord(20, 0, 20),
			map.ControllingFaction);
		pirate.State.PursuitDirective = new FleetPursuitDirective("delivery-1", "player-pursuit");
		map.FleetRegistry.Add(pirate);

		var (planner, _, engine) = CreatePlanner(map, pirate.State.Id);
		var first = Assert.IsType<PursueContactAction>(planner.Plan(pirate.State.Id));
		engine.Commit(first);
		player.State.Travel = new FleetTravel.AtRest(new Coord(8, 0, 8));

		var replanned = Assert.IsType<PursueContactAction>(planner.Plan(pirate.State.Id));
		Assert.Equal(new Coord(8, 0, 8), replanned.Destination);
	}

	[Fact]
	public void Plan_MovingTargetDoesNotPathfindEveryTick()
	{
		var map = maps.Fresh(11);
		StarSystemTestHarness.AddPlayerFleet(map, "player-pursuit");
		var player = map.FleetRegistry.FleetOf("player-pursuit");
		player.State.Travel = new FleetTravel.AtRest(new Coord(0, 0, 0));
		var pirate = StarSystemTestHarness.CreatePirateFleet(
			"pirate-pursuit",
			new Coord(200, 0, 200),
			map.ControllingFaction);
		pirate.State.PursuitDirective = new FleetPursuitDirective("delivery-1", "player-pursuit");
		map.FleetRegistry.Add(pirate);
		var pathfinder = new CountingPathfinder();
		var (planner, _, engine) = CreatePlanner(map, pirate.State.Id, pathfinder);
		var first = Assert.IsType<PursueContactAction>(planner.Plan(pirate.State.Id));
		engine.Commit(first);
		player.State.Travel = new FleetTravel.AtRest(new Coord(10, 0, 10));

		engine.AdvanceTick();
		var replanned = planner.Plan(pirate.State.Id);
		if (replanned is not null)
			engine.Commit(replanned);

		for (var tick = 0; tick < 5; tick++)
		{
			engine.AdvanceTick();
			Assert.Null(planner.Plan(pirate.State.Id));
		}
	}

	[Fact]
	public void PlanInterceptCourse_TargetInTransit_LeadsTargetAlongCommittedJourney()
	{
		var map = maps.Fresh(11);
		StarSystemTestHarness.AddPlayerFleet(map, "player-pursuit");
		var target = map.FleetRegistry.FleetOf("player-pursuit");
		target.State.Travel = new FleetTravel.AtRest(new Coord(20, 0, 0));
		var pirate = StarSystemTestHarness.CreatePirateFleet(
			"pirate-pursuit",
			new Coord(0, 0, 0),
			map.ControllingFaction);
		map.FleetRegistry.Add(pirate);

		var (planner, _, engine) = CreatePlanner(map, pirate.State.Id);
		var targetPath = TransitPath.FromPoints(
			[new Coord(20, 0, 0), new Coord(200, 0, 0)],
			[1.0, 1.0]);
		engine.Commit(new MoveAction(
			target.State.Id,
			target.State.Id,
			new Coord(200, 0, 0),
			targetPath));

		var course = planner.PlanInterceptCourse(
			pirate.State.Id,
			target.State.Id,
			EContactIntent.Engagement);

		Assert.NotNull(course);
		Assert.True(course.Destination.X > 20);
		Assert.True(course.Destination.X < 200);
	}

	[Fact]
	public void Plan_ReplansEngagementPursuitWhenTargetStartsNewJourney()
	{
		var map = maps.Fresh(11);
		StarSystemTestHarness.AddPlayerFleet(map, "player-pursuit");
		var player = map.FleetRegistry.FleetOf("player-pursuit");
		var pirate = StarSystemTestHarness.CreatePirateFleet(
			"pirate-pursuit",
			new Coord(0, 0, 0),
			map.ControllingFaction);
		map.FleetRegistry.Add(pirate);

		var (planner, _, engine) = CreatePlanner(map, pirate.State.Id);
		player.State.Travel = new FleetTravel.AtRest(new Coord(50, 0, 0));
		var targetPath = TransitPath.FromPoints(
			[new Coord(50, 0, 0), new Coord(100, 0, 0)],
			[1.0, 1.0]);
		engine.Commit(new MoveAction(
			player.State.Id,
			player.State.Id,
			new Coord(100, 0, 0),
			targetPath));
		new SetEngagementIntentEffect(pirate.State.Id, player.State.Id)
			.Apply(map, engine.ActorRuntimes.For(pirate.State.Id), pirate.State.Id);
		var first = Assert.IsType<PursueContactAction>(planner.Plan(pirate.State.Id));
		engine.Commit(first);

		var turnPath = TransitPath.FromPoints(
			[player.State.PositionAt(map, engine.ActorRuntimes.For(player.State.Id).CachedPath, 0).Position, new Coord(0, 0, 100)],
			[1.0, 1.0]);
		engine.Commit(new MoveAction(
			player.State.Id,
			player.State.Id,
			new Coord(0, 0, 100),
			turnPath));
		engine.AdvanceTick();

		var replanned = Assert.IsType<PursueContactAction>(planner.Plan(pirate.State.Id));
		Assert.NotEqual(first.Destination, replanned.Destination);
	}

	[Fact]
	public void Plan_DeferredReplan_SkipsInterceptPathfindingWhileTargetSteadyOnCourse()
	{
		var map = maps.Fresh(11);
		StarSystemTestHarness.AddPlayerFleet(map, "player-pursuit");
		var player = map.FleetRegistry.FleetOf("player-pursuit");
		player.State.Travel = new FleetTravel.AtRest(new Coord(50, 0, 0));
		var targetPath = TransitPath.FromPoints(
			[new Coord(50, 0, 0), new Coord(100, 0, 0)],
			[1.0, 1.0]);
		var pirate = StarSystemTestHarness.CreatePirateFleet(
			"pirate-pursuit",
			new Coord(0, 0, 0),
			map.ControllingFaction);
		pirate.State.PursuitDirective = new FleetPursuitDirective("delivery-1", "player-pursuit");
		map.FleetRegistry.Add(pirate);
		var pathfinder = new CountingPathfinder();
		var (planner, _, engine) = CreatePlanner(map, pirate.State.Id, pathfinder);
		engine.Commit(new MoveAction(
			player.State.Id,
			player.State.Id,
			new Coord(100, 0, 0),
			targetPath));
		var first = Assert.IsType<PursueContactAction>(planner.Plan(pirate.State.Id));
		engine.Commit(first);
		var callsAfterFirstPlan = pathfinder.CallCount;

		for (var tick = 0; tick < 3; tick++)
		{
			engine.AdvanceTick();
			Assert.Null(planner.Plan(pirate.State.Id));
		}

		Assert.Equal(callsAfterFirstPlan, pathfinder.CallCount);
	}

	[Fact]
	public void NotifyFleetMoved_ForcesReplanWithoutTickThrottle()
	{
		var map = maps.Fresh(11);
		StarSystemTestHarness.AddPlayerFleet(map, "player-pursuit");
		var player = map.FleetRegistry.FleetOf("player-pursuit");
		player.State.Travel = new FleetTravel.AtRest(new Coord(50, 0, 0));
		var targetPath = TransitPath.FromPoints(
			[new Coord(50, 0, 0), new Coord(100, 0, 0)],
			[1.0, 1.0]);
		var pirate = StarSystemTestHarness.CreatePirateFleet(
			"pirate-pursuit",
			new Coord(0, 0, 0),
			map.ControllingFaction);
		pirate.State.PursuitDirective = new FleetPursuitDirective("delivery-1", "player-pursuit");
		map.FleetRegistry.Add(pirate);
		var pathfinder = new CountingPathfinder();
		var (planner, _, engine) = CreatePlanner(map, pirate.State.Id, pathfinder);
		engine.Commit(new MoveAction(
			player.State.Id,
			player.State.Id,
			new Coord(100, 0, 0),
			targetPath));
		var first = Assert.IsType<PursueContactAction>(planner.Plan(pirate.State.Id));
		engine.Commit(first);
		var callsAfterFirstPlan = pathfinder.CallCount;

		PursuitPlanner.NotifyFleetMoved(
			map,
			id => engine.ActorRuntimes.For(id),
			player.State.Id);
		engine.Commit(new MoveAction(
			player.State.Id,
			player.State.Id,
			new Coord(0, 0, 100),
			TransitPath.FromPoints(
				[player.State.PositionAt(map, engine.ActorRuntimes.For(player.State.Id).CachedPath, 0).Position, new Coord(0, 0, 100)],
				[1.0, 1.0])));

		var replanned = Assert.IsType<PursueContactAction>(planner.Plan(pirate.State.Id));
		Assert.NotEqual(first.Destination, replanned.Destination);
		Assert.True(pathfinder.CallCount > callsAfterFirstPlan);
	}

	[Fact]
	public void Plan_DirectiveTargetAtDock_Waits()
	{
		var map = maps.Fresh(11);
		StarSystemTestHarness.AddPlayerFleet(map, "player-pursuit");
		var pirate = StarSystemTestHarness.CreatePirateFleet(
			"pirate-pursuit",
			new Coord(20, 0, 20),
			map.ControllingFaction);
		pirate.State.PursuitDirective = new FleetPursuitDirective("delivery-1", "player-pursuit");
		map.FleetRegistry.Add(pirate);

		var (planner, _, _) = CreatePlanner(map, pirate.State.Id);

		Assert.Null(planner.Plan(pirate.State.Id));
	}

	private static (
		PursuitPlanner Planner,
		ActionBatchSink Sink,
		Engine<StarMap, ActorRuntime> Engine) CreatePlanner(
		StarMap map,
		string actorId,
		IPathfinder? pathfinder = null)
	{
		var actorRuntimes = new ActorRuntimes<ActorRuntime>();
		actorRuntimes.For(actorId);
		actorRuntimes.For("player-pursuit");
		var engine = new Engine<StarMap, ActorRuntime>(map, actorRuntimes);
		var sink = new ActionBatchSink();
		var planner = new PursuitPlanner(
			() => engine.World,
			id => engine.ActorRuntimes.For(id),
			pathfinder ?? new StraightLinePathfinder());
		return (planner, sink, engine);
	}

	private sealed class StraightLinePathfinder : IPathfinder
	{
		public PathfindingResult FindPath(Coord origin, Coord destination) =>
			new PathfindingResult.Found(
				TransitPath.FromPoints([origin, destination], [1.0, 1.0]));
	}

	private sealed class CountingPathfinder : IPathfinder
	{
		public int CallCount { get; private set; }

		public PathfindingResult FindPath(Coord origin, Coord destination)
		{
			CallCount++;
			return new PathfindingResult.Found(
				TransitPath.FromPoints([origin, destination], [1.0, 1.0]));
		}
	}
}
