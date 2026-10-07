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
public sealed class TrafficExecutionAgentTests(StarMapFixture maps)
{
	[Fact]
	public void PlanAndPublish_ReadyUnitPublishesMove()
	{
		var map = maps.Fresh(42);
		var unit = map.FleetRegistry.All.First(candidate => candidate.State.HasChoreAtDock(map));
		var (agent, sink) = CreateAgent(map, unit.State.Id);

		agent.SetCanWork(true);
		agent.PlanAndPublish();

		Assert.True(sink.TryTakeBatch(unit.State.Id, out var batch));
		Assert.IsType<MoveAction>(Assert.Single(batch.Actions));
	}

	[Fact]
	public void PlanAndPublish_UsesCurrentLiveState()
	{
		var map = maps.Fresh(42);
		var unit = map.FleetRegistry.All.First(candidate => candidate.State.HasChoreAtDock(map));
		var (agent, sink) = CreateAgent(map, unit.State.Id);

		agent.SetCanWork(true);
		agent.PlanAndPublish();
		Assert.True(sink.TryTakeBatch(unit.State.Id, out _));

		AssignWork(map, unit);

		agent.PlanAndPublish();
		Assert.False(sink.TryTakeBatch(unit.State.Id, out var batch));
	}

	[Fact]
	public void PlanAndPublish_WaitsForScheduledBeginWork()
	{
		var map = maps.Fresh(42);
		var unit = map.FleetRegistry.All.First(candidate => candidate.State.HasChoreAtDock(map));
		var dockId = map.DockAt(unit.State)!.Id;
		var poiId = map.DocksById[dockId].PoiId;
		var (agent, sink) = CreateAgent(map, unit.State.Id);

		WorkScheduler.ReserveOnArrival(map, unit.State.Id, dockId);

		agent.SetCanWork(true);
		agent.PlanAndPublish();

		Assert.False(sink.TryTakeBatch(unit.State.Id, out _));
	}

	[Fact]
	public void PlanAndPublish_WorkingUnitPublishesNothing()
	{
		var map = maps.Fresh(42);
		var unit = map.FleetRegistry.All.First(candidate => candidate.State.HasChoreAtDock(map));
		var (agent, sink) = CreateAgent(map, unit.State.Id);

		AssignWork(map, unit);

		agent.SetCanWork(true);
		agent.PlanAndPublish();

		Assert.False(sink.TryTakeBatch(unit.State.Id, out _));
	}

	private static void AssignWork(StarMap map, Fleet unit)
	{
		var dock = map.DockAt(unit.State)!;
		map.Timeline.Schedule(
			1,
			new CompleteWorkAction(
				unit.State.Id,
				unit.State.Id,
				dock.PoiId,
				map.Timeline.Clock.Current));
	}

	private static (TrafficExecutionAgent Agent, ActionBatchSink Sink) CreateAgent(
		StarMap map,
		string actorId,
		IPathfinder? pathfinder = null)
	{
		var actorRuntimes = new ActorRuntimes<ActorRuntime>();
		actorRuntimes.For(actorId);
		var engine = new Engine<StarMap, ActorRuntime>(map, actorRuntimes);
		var sink = new ActionBatchSink();
		var agent = new TrafficExecutionAgent(
			() => engine.World,
			id => engine.ActorRuntimes.For(id),
			pathfinder ?? new StraightLinePathfinder());

		agent.Init(actorId, sink.WriterFor(actorId));
		return (agent, sink);
	}

	private sealed class StraightLinePathfinder : IPathfinder
	{
		public PathfindingResult FindPath(Coord origin, Coord destination) =>
			new PathfindingResult.Found(
				TransitPath.FromPoints([origin, destination], [1.0, 1.0]));
	}
}
