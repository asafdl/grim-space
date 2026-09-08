using GrimSpace.Core.Actions;
using GrimSpace.Core.Engine;
using GrimSpace.Math.Grid;
using GrimSpace.World.StarSystem;
using GrimSpace.World.StarSystem.Actions;
using GrimSpace.World.StarSystem.Pathfinding;
using GrimSpace.World.StarSystem.Runtime;

namespace GrimSpace.Tests.Engine;

public sealed class ExecutionAgentTests
{
	[Fact]
	public async Task WorldUpdatedAfterPublishRepublishesWhenCanWork()
	{
		var agent = new TestExecutionAgent();
		var map = StarMap.CreateDevDefault(42);
		var unit = map.UnitRegistry.All.First(u => u.State.IsReadyToDepart);
		var actorRuntimes = new ActorRuntimes<ActorRuntime>();
		actorRuntimes.For(unit.State.Id);

		var engine = new Engine<StarMap, ActorRuntime>(map, actorRuntimes);
		var sink = new ActionBatchSink();
		ExecutionAgent<StarMap, ActorRuntime>.Initialize(
			agent,
			unit.State.Id,
			engine.CreateSimulation,
			sink.WriterFor(unit.State.Id));
		agent.SetCanWork(true);

		var origin = map.DocksById[unit.State.DockedAtDockId].Position;
		var destination = map.DocksById[unit.State.NextChoreDockId()].Position;
		var path = TransitPath.FromPoints([origin, destination], [1.0, 1.0]);
		agent.PublishForTest([new MoveAction(unit.State.Id, unit.State.Id, destination, path)]);

		var first = await sink.WaitForBatchAsync(unit.State.Id);
		Assert.True(first.IsSuccess);
		Assert.Single(first.Batch!.Actions);

		agent.OnWorldUpdated();
		var second = await sink.WaitForBatchAsync(unit.State.Id);
		Assert.True(second.IsSuccess);
		Assert.Empty(second.Batch!.Actions);
	}

	[Fact]
	public async Task FailSurfacesThroughSink()
	{
		var agent = new TestExecutionAgent();
		var map = StarMap.CreateDevDefault(42);
		var unit = map.UnitRegistry.All.First(u => u.State.IsReadyToDepart);
		var actorRuntimes = new ActorRuntimes<ActorRuntime>();
		actorRuntimes.For(unit.State.Id);

		var engine = new Engine<StarMap, ActorRuntime>(map, actorRuntimes);
		var sink = new ActionBatchSink();
		ExecutionAgent<StarMap, ActorRuntime>.Initialize(
			agent,
			unit.State.Id,
			engine.CreateSimulation,
			sink.WriterFor(unit.State.Id));
		agent.SetCanWork(true);

		var failure = new InvalidOperationException("agent failed");
		agent.FailForTest(failure);

		var result = await sink.WaitForBatchAsync(unit.State.Id);
		Assert.False(result.IsSuccess);
		Assert.Same(failure, result.Failure);
	}

	private sealed class TestExecutionAgent : SimulationExecutionAgent<StarMap, ActorRuntime>
	{
		protected override bool PublishOnActivate => false;

		protected override void ProduceActionsJob(Simulation<StarMap, ActorRuntime> simulation)
		{
			TryPublishIfReady();
		}

		public void PublishForTest(IReadOnlyList<IAction> actions) => Publish(actions);

		public void FailForTest(Exception exception) => Fail(exception);
	}
}
