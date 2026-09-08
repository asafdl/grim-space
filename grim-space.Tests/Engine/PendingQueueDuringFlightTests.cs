using GrimSpace.Core.Actions;
using GrimSpace.Core.Engine;
using GrimSpace.Math.Grid;
using GrimSpace.World.StarSystem;
using GrimSpace.World.StarSystem.Actions;
using GrimSpace.World.StarSystem.Pathfinding;
using GrimSpace.World.StarSystem.Runtime;
using GrimSpace.Tests.World.StarSystem;

namespace GrimSpace.Tests.Engine;

public sealed class PendingQueueDuringFlightTests(DevStarMapFixture maps)
{
	[Fact]
	public async Task PushWhileBatchInFlightStaysPendingUntilAck()
	{
		var agent = new RealtimeTestExecutionAgent();
		var map = maps.Fresh(42);
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
		var firstDestination = map.DocksById[unit.State.NextChoreDockId()].Position;
		var secondDestination = firstDestination + Coord.Forward;
		var firstPath = TransitPath.FromPoints([origin, firstDestination], [1.0, 1.0]);
		var secondPath = TransitPath.FromPoints([origin, secondDestination], [1.0, 1.0]);

		Assert.True(agent.QueueForTest(new MoveAction(unit.State.Id, unit.State.Id, firstDestination, firstPath)));
		Assert.True(sink.TryTakeBatch(unit.State.Id, out var firstBatch));
		Assert.Single(firstBatch.Actions);

		Assert.True(agent.QueueForTest(new MoveAction(unit.State.Id, unit.State.Id, secondDestination, secondPath)));
		Assert.False(sink.TryTakeBatch(unit.State.Id, out _));

		agent.OnWorldUpdated();
		var secondResult = await sink.WaitForBatchAsync(unit.State.Id);
		Assert.True(secondResult.IsSuccess);
		Assert.Single(secondResult.Batch!.Actions);
	}

	private sealed class RealtimeTestExecutionAgent : SimulationExecutionAgent<StarMap, ActorRuntime>
	{
		protected override bool PublishOnActivate => false;

		protected override void ProduceActionsJob(Simulation<StarMap, ActorRuntime> simulation)
		{
		}

		public bool QueueForTest(IAction action) => PushPending(action);
	}
}
