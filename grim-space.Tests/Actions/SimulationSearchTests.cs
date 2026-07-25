using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Board;
using GrimSpace.Battle.Planning;
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
		var battle = BattleTestFixture.BeginPlanning(new Coord(5, 5, 5));
		var session = battle.Session;
		var enemyId = battle.Opponent.State.Id;
		var railgun = new RailgunAction(PlayerId, enemyId);

		Assert.Equal(CombatConfig.RailgunsPerTurn, session.PreviewWorld.StateOf(PlayerId).RailgunRemaining);
		Assert.True(session.TryEnqueue(railgun));
		Assert.Equal(CombatConfig.RailgunsPerTurn - 1, session.PreviewWorld.StateOf(PlayerId).RailgunRemaining);
		Assert.False(session.TryEnqueue(new RailgunAction(PlayerId, enemyId)));
	}

	[Fact]
	public void FlakBudgetEnforcedBySimulationTryEnqueue()
	{
		var battle = BattleTestFixture.BeginPlanning(new Coord(5, 5, 5));
		var session = battle.Session;

		Assert.True(session.TryEnqueue(new FlakAction(PlayerId, EFlakMount.Port)));
		Assert.False(session.TryEnqueue(new FlakAction(PlayerId, EFlakMount.Starboard)));
	}

	[Fact]
	public void MoveOnlySearch_StaysWithinDepthLimit()
	{
		const int expectedMaxDepth = 12;
		var battle = BattleTestFixture.BeginPlanning(new Coord(5, 5, 5));
		var maxDepth = 0;

		foreach (var frame in battle.Session.SearchMoves(PlayerId))
			maxDepth = System.Math.Max(maxDepth, frame.Depth);

		Assert.True(maxDepth <= expectedMaxDepth, maxDepth.ToString());
	}

	[Fact]
	public void SearchWithQueuedActionsDoesNotMutateSession()
	{
		var origin = new Coord(5, 5, 5);
		var battle = BattleTestFixture.BeginPlanning(origin);
		var session = battle.Session;
		var heading = HeadingDef.Instance.Bind(PlayerId, GrimSpace.Battle.Movement.Enums.EHeadingTurn.YawRight);

		Assert.True(session.TryEnqueue(heading));
		var actionsBefore = session.Actions.ToList();
		var apBefore = session.PreviewWorld.StateOf(PlayerId).ActionPoints;

		var foundExtension = false;
		foreach (var frame in session.SearchMoves(PlayerId))
		{
			if (frame.Actions.Count > actionsBefore.Count)
			{
				foundExtension = true;
				break;
			}
		}

		Assert.Equal(actionsBefore, session.Actions);
		Assert.Equal(apBefore, session.PreviewWorld.StateOf(PlayerId).ActionPoints);
		Assert.True(foundExtension);
	}
}
