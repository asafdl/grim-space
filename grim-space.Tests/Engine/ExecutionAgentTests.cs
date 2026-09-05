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
	public void TakeCompletedActions_ReturnsCompletedBatch()
	{
		var agent = new TestExecutionAgent();
		var map = StarMap.CreateDevDefault(42);
		var unit = map.UnitRegistry.All.First();
		var actorRuntimes = new ActorRuntimes<ActorRuntime>();
		actorRuntimes.For(unit.State.Id);

		var engine = new Engine<StarMap, ActorRuntime>(map, actorRuntimes);
		Action<string?>? activate = null;
		agent.Init(unit.State.Id, engine.CreateSimulation, register => activate = register);
		activate!(unit.State.Id);

		var origin = map.DocksById[unit.State.DockedAtDockId].Position;
		var destination = map.DocksById[unit.State.NextChoreDockId()].Position;
		var path = TransitPath.FromPoints([origin, destination], [1.0, 1.0]);
		var actions = new IAction[]
		{
			new MoveAction(unit.State.Id, unit.State.Id, destination, path),
		};
		agent.CompleteForTest(actions);

		Assert.Single(agent.TakeCompletedActions());
	}

	[Fact]
	public void TakeCompletedActions_ThrowsWhenStillPending()
	{
		var agent = new PendingExecutionAgent();
		var map = StarMap.CreateDevDefault(42);
		var unit = map.UnitRegistry.All.First();
		var actorRuntimes = new ActorRuntimes<ActorRuntime>();
		actorRuntimes.For(unit.State.Id);

		var engine = new Engine<StarMap, ActorRuntime>(map, actorRuntimes);
		Action<string?>? activate = null;
		agent.Init(unit.State.Id, engine.CreateSimulation, register => activate = register);
		activate!(unit.State.Id);

		Assert.Throws<InvalidOperationException>(() => agent.TakeCompletedActions());
	}

	[Fact]
	public void TakeCompletedActions_PropagatesAgentFailure()
	{
		var agent = new TestExecutionAgent();
		var map = StarMap.CreateDevDefault(42);
		var unit = map.UnitRegistry.All.First();
		var actorRuntimes = new ActorRuntimes<ActorRuntime>();
		actorRuntimes.For(unit.State.Id);

		var engine = new Engine<StarMap, ActorRuntime>(map, actorRuntimes);
		Action<string?>? activate = null;
		agent.Init(unit.State.Id, engine.CreateSimulation, register => activate = register);
		activate!(unit.State.Id);

		var failure = new InvalidOperationException("agent failed");
		agent.FailForTest(failure);

		var thrown = Assert.Throws<InvalidOperationException>(() => agent.TakeCompletedActions());
		Assert.Same(failure, thrown);
	}

	private sealed class TestExecutionAgent : ExecutionAgent<StarMap, ActorRuntime>
	{
		protected override void ProduceActionsJob(Simulation<StarMap, ActorRuntime> simulation)
		{
		}

		public void CompleteForTest(IReadOnlyList<IAction> actions) => Complete(actions);

		public void FailForTest(Exception exception) => Fail(exception);
	}

	private sealed class PendingExecutionAgent : ExecutionAgent<StarMap, ActorRuntime>
	{
		protected override void ProduceActionsJob(Simulation<StarMap, ActorRuntime> simulation)
		{
		}
	}
}
