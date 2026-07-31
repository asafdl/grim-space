using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Ai;
using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.Weapons;
using GrimSpace.Core.Actions;
using GrimSpace.Core.Engine;
using GrimSpace.Core.Log;
using GrimSpace.Math.Grid;

namespace GrimSpace.Tests.Actions;

public sealed class SimulationSearchTests
{
	private const string PlayerId = "player";

	[Fact]
	public void RailgunBudgetEnforcedBySimulationTryEnqueue()
	{
		var battle = BattleTestFixture.BeginSimulation(new Coord(5, 5, 5));
		var session = battle.Sim;
		var railgun = new RailgunAction(PlayerId);

		Assert.Equal(CombatConfig.RailgunsPerTurn, session.StateOf<ActorState>(PlayerId).RailgunRemaining);
		Assert.True(session.TryEnqueue(railgun));
		Assert.Equal(CombatConfig.RailgunsPerTurn - 1, session.StateOf<ActorState>(PlayerId).RailgunRemaining);
		Assert.False(session.TryEnqueue(new RailgunAction(PlayerId)));
	}

	[Fact]
	public void FlakBudgetEnforcedBySimulationTryEnqueue()
	{
		var battle = BattleTestFixture.BeginSimulation(new Coord(5, 5, 5));
		var session = battle.Sim;

		Assert.True(session.TryEnqueue(new FlakAction(PlayerId, EFlakMount.Port)));
		Assert.False(session.TryEnqueue(new FlakAction(PlayerId, EFlakMount.Starboard)));
	}

	[Fact]
	public void PeekReturnsNullForIllegalAction()
	{
		var battle = BattleTestFixture.BeginSimulation(new Coord(5, 5, 5));
		var session = battle.Sim;

		Assert.True(session.TryEnqueue(new RailgunAction(PlayerId)));
		Assert.Null(session.Peek(new RailgunAction(PlayerId)));
	}

	[Fact]
	public void PeekReturnsFrameForLegalActionWithoutMutatingQueue()
	{
		var battle = BattleTestFixture.BeginSimulation(new Coord(5, 5, 5));
		var session = battle.Sim;
		var railgun = new RailgunAction(PlayerId);

		var peek = session.Peek(railgun);
		Assert.NotNull(peek);
		Assert.Empty(session.Actions);
		Assert.Equal(CombatConfig.RailgunsPerTurn, session.StateOf<ActorState>(PlayerId).RailgunRemaining);
	}

	[Fact]
	public void MoveOnlySearch_StaysWithinDepthLimit()
	{
		const int expectedMaxDepth = 12;
		var battle = BattleTestFixture.BeginSimulation(new Coord(5, 5, 5));
		var maxDepth = 0;

		foreach (var frame in battle.Sim.Search(PlayerId, [MoveDef.Instance], BattleSearchVisit.ForCapabilities))
			maxDepth = System.Math.Max(maxDepth, frame.Depth);

		Assert.True(maxDepth <= expectedMaxDepth, maxDepth.ToString());
	}

	[Fact]
	public void SearchWithQueuedActionsDoesNotMutateSimulation()
	{
		var origin = new Coord(5, 5, 5);
		var battle = BattleTestFixture.BeginSimulation(origin);
		var session = battle.Sim;
		var heading = HeadingDef.Instance.Bind(PlayerId, GrimSpace.Battle.Movement.Enums.EHeadingTurn.YawRight);

		Assert.True(session.TryEnqueue(heading));
		var actionsBefore = session.Actions.ToList();
		var apBefore = session.StateOf<ActorState>(PlayerId).ActionPoints;

		var foundExtension = false;
		foreach (var frame in session.Search(PlayerId, [MoveDef.Instance], BattleSearchVisit.ForCapabilities))
		{
			if (frame.Actions.Count > actionsBefore.Count)
			{
				foundExtension = true;
				break;
			}
		}

		Assert.Equal(actionsBefore, session.Actions);
		Assert.Equal(apBefore, session.StateOf<ActorState>(PlayerId).ActionPoints);
		Assert.True(foundExtension);
	}
}
