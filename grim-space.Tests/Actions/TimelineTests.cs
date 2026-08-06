using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Movement.Enums;
using GrimSpace.Core.Engine;
using GrimSpace.Math.Grid;

namespace GrimSpace.Tests.Actions;

public sealed class TimelineTests
{
	[Fact]
	public void CommitRecordsHistoryByActor()
	{
		var battle = BattleTestFixture.BeginSimulation(new Coord(5, 5, 5));
		var tick = battle.TurnNumber;
		Assert.True(battle.Sim.TryEnqueue(new HeadingTurnAction(battle.PlayerId, EHeadingTurn.YawRight)));
		battle.ResolveTurn();

		var byActor = battle.Engine.HistoryByActor(tick);
		Assert.True(byActor.ContainsKey(battle.PlayerId));
		Assert.Contains(byActor[battle.PlayerId], action => action is HeadingTurnAction or EndOfPhaseAction);
	}

	[Fact]
	public void ScheduleAppliesOnAdvanceTick()
	{
		var timeline = new Timeline();
		timeline.Clock.Set(1);
		var action = new HeadingTurnAction("player", EHeadingTurn.YawRight);
		timeline.Schedule(1, action);

		Assert.Empty(timeline.History(1));
		Assert.Empty(timeline.TakePending(1));

		timeline.Clock.Next();
		Assert.Equal(action, Assert.Single(timeline.TakePending()));
	}

	[Fact]
	public void ClonePreservesHistoryAndPending()
	{
		var timeline = new Timeline();
		timeline.Clock.Set(1);
		timeline.Record([new HeadingTurnAction("a", EHeadingTurn.YawRight)], "a");
		timeline.Schedule(1, new HeadingTurnAction("b", EHeadingTurn.YawLeft));

		var clone = timeline.Clone();
		Assert.Equal(1, clone.Clock.Current);
		Assert.Single(clone.History(1));
		clone.Clock.Next();
		Assert.Single(clone.TakePending());
	}
}
