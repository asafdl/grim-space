using GrimSpace.Core.Engine;
using GrimSpace.Math.Grid;
using GrimSpace.World.StarSystem;
using GrimSpace.World.StarSystem.Actions;
using GrimSpace.World.StarSystem.Agents;
using GrimSpace.World.StarSystem.Pathfinding;
using GrimSpace.World.StarSystem.Runtime;
using GrimSpace.World.StarSystem.Units;
using GrimSpace.Tests.World.StarSystem;

namespace GrimSpace.Tests.World.StarSystem.Traffic;

[StarSystemTestSuite]
public sealed class PatrolExecutionAgentTests(StarMapFixture maps)
{
	private static readonly Coord PatrolOrigin = new(-1000, 0, -1000);

	[Fact]
	public void PlanAndPublish_PublishesFourLongPatrolLegsAsOneRoute()
	{
		var (agent, sink, _, unit) = CreateAgent(maps.Fresh(42));

		agent.SetCanWork(true);
		agent.PlanAndPublish();

		Assert.True(sink.TryTakeBatch(unit.State.Id, out var batch));
		var move = Assert.IsType<MoveAction>(Assert.Single(batch.Actions));
		Assert.Equal(4, move.Path.Legs.Length);

		var legOrigin = PatrolOrigin;
		foreach (var leg in move.Path.Legs)
		{
			var destination = leg.Points[^1];
			Assert.Equal(legOrigin, leg.Points[0]);
			AssertMinimumPatrolDistance(legOrigin, destination, unit.State.PatrolRadius);
			legOrigin = destination;
		}

		Assert.Equal(legOrigin, move.Destination);
	}

	[Fact]
	public void AdvanceTick_ReplansPatrolOnArrivalTick()
	{
		var map = maps.Fresh(42);
		var unit = AddPatrolUnit(map);
		var orchestrator = StarSystemOrchestrator.FromMap(map, new StraightLinePathfinder());

		orchestrator.AdvanceTick();
		Assert.Equal(EPhase.InTransit, unit.State.Phase);
		var firstJourneyId = unit.State.Journey.JourneyId;
		var firstDestination = unit.State.Journey.Destination;
		var firstPath = orchestrator.RuntimeFor(unit.State.Id).CachedPath!;

		for (var i = 0; i < firstPath.DurationTicks(unit.State.SpeedPerTick); i++)
			orchestrator.AdvanceTick();

		Assert.Equal(EPhase.InTransit, unit.State.Phase);
		Assert.NotEqual(firstJourneyId, unit.State.Journey.JourneyId);
		Assert.Equal(firstDestination, unit.State.Journey.Origin);
		Assert.Equal(orchestrator.Tick, unit.State.Journey.StartTick);
	}

	[Fact]
	public void AdvanceTick_AfterPatrolFleetRemoved_RetiresAgent()
	{
		var map = maps.Fresh(42);
		var unit = AddPatrolUnit(map);
		var orchestrator = StarSystemOrchestrator.FromMap(map, new StraightLinePathfinder());
		orchestrator.AdvanceTick();
		Assert.True(map.FleetRegistry.Remove(unit.State.Id));

		var exception = Record.Exception(orchestrator.AdvanceTick);

		Assert.Null(exception);
	}

	[Fact]
	public void PlanAndPublish_WaitsForArrivalThenSelectsAnotherDestination()
	{
		var (agent, sink, engine, unit) = CreateAgent(maps.Fresh(42));

		agent.SetCanWork(true);
		agent.PlanAndPublish();
		Assert.True(sink.TryTakeBatch(unit.State.Id, out var firstBatch));
		var firstMove = Assert.IsType<MoveAction>(Assert.Single(firstBatch.Actions));
		engine.Commit(firstMove);

		agent.PlanAndPublish();
		Assert.False(sink.TryTakeBatch(unit.State.Id, out _));

		for (var i = 0; i < firstMove.Path.DurationTicks(unit.State.SpeedPerTick); i++)
			engine.AdvanceTick();
		Assert.Equal(EPhase.Docked, unit.State.Phase);
		Assert.Equal(firstMove.Destination, unit.State.IdleCoord);

		agent.PlanAndPublish();

		Assert.True(sink.TryTakeBatch(unit.State.Id, out var secondBatch));
		var secondMove = Assert.IsType<MoveAction>(Assert.Single(secondBatch.Actions));
		Assert.Equal(firstMove.Destination, secondMove.Path.Legs[0].Points[0]);
		AssertMinimumPatrolDistance(
			firstMove.Destination,
			secondMove.Path.Legs[0].Points[^1],
			unit.State.PatrolRadius);
	}

	private static void AssertMinimumPatrolDistance(Coord origin, Coord destination, int radius)
	{
		var displacement = destination - origin;
		Assert.True(
			16 * (displacement.X * displacement.X + displacement.Z * displacement.Z)
				>= 9 * radius * radius);
	}

	private static (
		PatrolExecutionAgent Agent,
		ActionBatchSink Sink,
		Engine<StarMap, ActorRuntime> Engine,
		Fleet Unit) CreateAgent(StarMap map)
	{
		var unit = AddPatrolUnit(map);
		var unitId = unit.State.Id;
		var actorRuntimes = new ActorRuntimes<ActorRuntime>();
		actorRuntimes.For(unitId);
		var engine = new Engine<StarMap, ActorRuntime>(map, actorRuntimes);
		var sink = new ActionBatchSink();
		var agent = new PatrolExecutionAgent(
			() => engine.World,
			id => engine.ActorRuntimes.For(id),
			new StraightLinePathfinder());
		agent.Init(unitId, sink.WriterFor(unitId));
		return (agent, sink, engine, unit);
	}

	private static Fleet AddPatrolUnit(StarMap map)
	{
		const string unitId = "patrol-test";
		var unit = Factory.Create(new Spawn(
			unitId,
			EType.PirateFleet,
			"",
			PatrolOrigin,
			5.0,
			1.0,
			1.0,
			[],
			PatrolRadius: 8));
		map.FleetRegistry.Add(unit);
		return unit;
	}

	private sealed class StraightLinePathfinder : IPathfinder
	{
		public PathfindingResult FindPath(Coord origin, Coord destination) =>
			new PathfindingResult.Found(
				TransitPath.FromPoints([origin, destination], [1.0, 1.0]));
	}
}
