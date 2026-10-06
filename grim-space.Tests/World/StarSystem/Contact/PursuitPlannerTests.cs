using GrimSpace.Core.Engine;
using GrimSpace.Math.Grid;
using GrimSpace.World.StarSystem;
using GrimSpace.World.StarSystem.Actions;
using GrimSpace.World.StarSystem.Contact;
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
		player.State.DockedAtDockId = "";
		player.State.IdleCoord = new Coord(5, 0, 5);
		var pirate = StarSystemTestHarness.CreatePirateFleet(
			"pirate-pursuit",
			new Coord(20, 0, 20),
			map.ControllingFaction);
		pirate.State.PursuitDirective = new FleetPursuitDirective("delivery-1", "player-pursuit");
		map.FleetRegistry.Add(pirate);

		var (planner, _, engine) = CreatePlanner(map, pirate.State.Id);
		var first = Assert.IsType<PursueContactAction>(planner.Plan(pirate.State.Id));
		engine.Commit(first);
		player.State.IdleCoord = new Coord(8, 0, 8);

		Assert.Null(planner.Plan(pirate.State.Id));
		engine.AdvanceTick();
		var replanned = Assert.IsType<PursueContactAction>(planner.Plan(pirate.State.Id));

		Assert.Equal(new Coord(8, 0, 8), replanned.Destination);
	}

	[Fact]
	public void Plan_MovingTargetDoesNotPathfindEveryTick()
	{
		var map = maps.Fresh(11);
		StarSystemTestHarness.AddPlayerFleet(map, "player-pursuit");
		var player = map.FleetRegistry.FleetOf("player-pursuit");
		player.State.DockedAtDockId = "";
		player.State.IdleCoord = new Coord(0, 0, 0);
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
		player.State.IdleCoord = new Coord(10, 0, 10);

		for (var tick = 0; tick < 5; tick++)
		{
			engine.AdvanceTick();
			Assert.Null(planner.Plan(pirate.State.Id));
		}

		Assert.Equal(1, pathfinder.CallCount);
	}

	[Fact]
	public void PlanInterceptCourse_TargetInTransit_LeadsTargetAlongCommittedJourney()
	{
		var map = maps.Fresh(11);
		StarSystemTestHarness.AddPlayerFleet(map, "player-pursuit");
		var target = map.FleetRegistry.FleetOf("player-pursuit");
		target.State.DockedAtDockId = "";
		target.State.IdleCoord = new Coord(20, 0, 0);
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
