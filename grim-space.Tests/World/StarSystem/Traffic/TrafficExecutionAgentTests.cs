using GrimSpace.Core.Engine;
using GrimSpace.Math.Grid;
using GrimSpace.World.StarSystem;
using GrimSpace.World.StarSystem.Actions;
using GrimSpace.World.StarSystem.Agents;
using GrimSpace.World.StarSystem.Pathfinding;
using GrimSpace.World.StarSystem.Runtime;
using GrimSpace.World.StarSystem.Units;

namespace GrimSpace.Tests.World.StarSystem.Traffic;

public sealed class TrafficExecutionAgentTests
{
	[Fact]
	public void PlanAndPublish_ReadyUnitPublishesMove()
	{
		var map = StarMap.CreateDevDefault(42);
		var unit = map.UnitRegistry.All.First(candidate => candidate.State.IsReadyToDepart);
		var (agent, sink) = CreateAgent(map, unit.State.Id);

		agent.SetCanWork(true);
		agent.PlanAndPublish();

		Assert.True(sink.TryTakeBatch(unit.State.Id, out var batch));
		Assert.IsType<MoveAction>(Assert.Single(batch.Actions));
	}

	[Fact]
	public void PlanAndPublish_UsesCurrentLiveState()
	{
		var map = StarMap.CreateDevDefault(42);
		var unit = map.UnitRegistry.All.First(candidate => candidate.State.IsReadyToDepart);
		var (agent, sink) = CreateAgent(map, unit.State.Id);

		agent.SetCanWork(true);
		agent.PlanAndPublish();
		Assert.True(sink.TryTakeBatch(unit.State.Id, out _));

		unit.State.Phase = EPhase.Working;

		agent.PlanAndPublish();
		Assert.False(sink.TryTakeBatch(unit.State.Id, out var batch));
	}

	[Fact]
	public void PlanAndPublish_WaitsForScheduledBeginWork()
	{
		var map = StarMap.CreateDevDefault(42);
		var unit = map.UnitRegistry.All.First(candidate => candidate.State.IsReadyToDepart);
		var dockId = unit.State.DockedAtDockId;
		var poiId = map.DocksById[dockId].PoiId;
		var (agent, sink) = CreateAgent(map, unit.State.Id);

		map.Timeline.Schedule(2, new BeginWorkAction(unit.State.Id, unit.State.Id, poiId, map.Timeline.Clock.Current + 2));

		agent.SetCanWork(true);
		agent.PlanAndPublish();

		Assert.False(sink.TryTakeBatch(unit.State.Id, out _));
	}

	[Fact]
	public void PlanAndPublish_WorkingUnitPublishesNothing()
	{
		var map = StarMap.CreateDevDefault(42);
		var unit = map.UnitRegistry.All.First(candidate => candidate.State.IsReadyToDepart);
		var (agent, sink) = CreateAgent(map, unit.State.Id);

		unit.State.Phase = EPhase.Working;

		agent.SetCanWork(true);
		agent.PlanAndPublish();

		Assert.False(sink.TryTakeBatch(unit.State.Id, out _));
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
