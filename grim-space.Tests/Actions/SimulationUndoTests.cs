using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.World;
using GrimSpace.Core.Actions;
using GrimSpace.Core.Engine;
using GrimSpace.Math.Grid;

namespace GrimSpace.Tests.Actions;

public sealed class SimulationUndoTests
{
	private const string PlayerId = "player";

	private static readonly IActionDef<IAction, BattleWorld, ActorRuntime, IEffect<BattleWorld, ActorRuntime>>[] MovementDefs =
	[
		MoveDef.Instance,
		HeadingDef.Instance,
		RollDef.Instance,
	];

	[Fact]
	public void DequeueRestoresWorldAndRuntimesMatchingReevaluate()
	{
		var battle = BattleTestFixture.BeginSimulation(new Coord(5, 5, 5));
		var sim = battle.PlayerAgent.Sim;
		var sequence = BuildRandomLegalSequence(sim, maxActions: 6, seed: 42);

		foreach (var action in sequence)
			Assert.True(sim.TryEnqueue(action));

		while (sim.Actions.Count > 0)
		{
			var expectedActions = sim.Actions.Take(sim.Actions.Count - 1).ToList();
			sim.Dequeue(sim.Actions.Count - 1);

			var reference = sim.ForkFromAnchor();
			foreach (var action in expectedActions)
				Assert.True(reference.TryEnqueue(action));

			AssertWorldAndRuntimesMatch(reference, sim);
		}
	}

	[Fact]
	public void TryUndoLastRestoresWorldAndRuntimesMatchingReevaluate()
	{
		var battle = BattleTestFixture.BeginSimulation(new Coord(5, 5, 5));
		var sim = battle.PlayerAgent.Sim;
		var sequence = BuildRandomLegalSequence(sim, maxActions: 5, seed: 7);

		foreach (var action in sequence)
			Assert.True(sim.TryEnqueue(action));

		while (sim.Actions.Count > 0)
		{
			var expectedActions = sim.Actions.Take(sim.Actions.Count - 1).ToList();
			Assert.True(sim.TryUndoLast());

			var reference = sim.ForkFromAnchor();
			foreach (var action in expectedActions)
				Assert.True(reference.TryEnqueue(action));

			AssertWorldAndRuntimesMatch(reference, sim);
		}
	}

	private static List<IAction> BuildRandomLegalSequence(
		Simulation<BattleWorld, ActorRuntime> sim,
		int maxActions,
		int seed)
	{
		var rng = new Random(seed);
		var sequence = new List<IAction>();
		var probe = sim.ForkFromAnchor();

		for (var i = 0; i < maxActions; i++)
		{
			var legal = LegalActionProbe.LegalActions(probe, PlayerId, MovementDefs);
			if (legal.Count == 0)
				break;

			var action = legal[rng.Next(legal.Count)];
			if (!probe.TryEnqueue(action))
				break;

			sequence.Add(action);
		}

		return sequence;
	}

	private static void AssertWorldAndRuntimesMatch(
		Simulation<BattleWorld, ActorRuntime> expected,
		Simulation<BattleWorld, ActorRuntime> actual)
	{
		Assert.Equal(expected.Actions.Count, actual.Actions.Count);
		Assert.Equal(
			expected.StateOf<ActorState>(PlayerId).Position,
			actual.StateOf<ActorState>(PlayerId).Position);
		Assert.Equal(
			expected.StateOf<ActorState>(PlayerId).ActionPoints,
			actual.StateOf<ActorState>(PlayerId).ActionPoints);
		Assert.Equal(
			expected.StateOf<ActorState>(PlayerId).MomentumLevel,
			actual.StateOf<ActorState>(PlayerId).MomentumLevel);
		Assert.Equal(
			expected.RuntimeFor(PlayerId).ActivePath?.EndPosition,
			actual.RuntimeFor(PlayerId).ActivePath?.EndPosition);
		Assert.Equal(expected.InvariantStatus, actual.InvariantStatus);
	}
}
