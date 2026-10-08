using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Effects;
using GrimSpace.Units.Maneuvering;
using GrimSpace.Battle.Units;
using GrimSpace.Battle.Abilities;
using GrimSpace.Battle.World;
using GrimSpace.Core.Actions;
using GrimSpace.Core.Engine;
using GrimSpace.Math.Grid;
using GrimSpace.Units;
using GrimSpace.Units.Enums;

namespace GrimSpace.Tests.Actions;

[BattleTestSuite]
public sealed class TimelineTests
{
	[Fact]
	public void CommitRecordsHistoryByActor()
	{
		var battle = BattleTestFixture.BeginSimulation(new Coord(5, 5, 5));
		var tick = battle.TurnNumber;
		Assert.True(battle.PlayerAgent.Sim.TryEnqueue(new MoveStepAction(battle.PlayerId)));
		BattleTestActions.CommitAndResolve(battle);

		var byActor = battle.Engine.HistoryByActor(tick);
		Assert.True(byActor.ContainsKey(battle.PlayerId));
		Assert.Contains(byActor[battle.PlayerId], action => action is MoveStepAction or EndOfPhaseAction);
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
	public void TrimHistory_RemovesTicksOlderThanRetentionWindow()
	{
		var timeline = new Timeline();
		timeline.Clock.Set(1);
		timeline.Append(new HeadingTurnAction("a", EHeadingTurn.YawRight));

		timeline.Clock.Next();
		timeline.Append(new HeadingTurnAction("b", EHeadingTurn.YawLeft));

		timeline.Clock.Next();
		var third = new HeadingTurnAction("c", EHeadingTurn.YawRight);
		timeline.Append(third);

		timeline.Clock.Set(1002);
		timeline.TrimHistory(retainTicks: 1000);

		Assert.Empty(timeline.History(1));
		Assert.Empty(timeline.History(2));
		Assert.Same(third, Assert.Single(timeline.History(3)));
	}

	[Fact]
	public void CloneSnapshot_CopiesClockAndPendingWithoutHistory()
	{
		var timeline = new Timeline();
		timeline.Clock.Set(5);
		timeline.Append(new HeadingTurnAction("a", EHeadingTurn.YawRight));
		var pending = new HeadingTurnAction("b", EHeadingTurn.YawLeft);
		timeline.Schedule(1, pending);

		var snapshot = timeline.CloneSnapshot();

		Assert.Equal(5, snapshot.Clock.Current);
		Assert.Empty(snapshot.History());
		Assert.Equal(pending, Assert.Single(snapshot.TakePending(6)));
	}

	[Fact]
	public void CaptureSnapshot_RestoresHistoryAndPendingWithoutDraining()
	{
		var source = new Timeline();
		source.Clock.Set(3);
		var committed = new HeadingTurnAction("a", EHeadingTurn.YawRight);
		var pending = new HeadingTurnAction("b", EHeadingTurn.YawLeft);
		source.Append(committed);
		source.Schedule(2, pending);

		var snapshot = source.ToSnapshot();
		var restored = Timeline.From(snapshot);

		Assert.Equal(3, restored.Clock.Current);
		Assert.Same(committed, Assert.Single(restored.History(3)));
		Assert.Same(pending, Assert.Single(restored.TakePending(5)));
		Assert.Same(committed, Assert.Single(source.History(3)));
		Assert.Same(pending, Assert.Single(source.TakePending(5)));
	}

	[Fact]
	public void DrainUntilRemovesAndReturnsHistoryBatchesInTickOrder()
	{
		var timeline = new Timeline();
		timeline.Clock.Set(1);
		var first = new HeadingTurnAction("a", EHeadingTurn.YawRight);
		timeline.Append(first);

		timeline.Clock.Next();
		var second = new HeadingTurnAction("b", EHeadingTurn.YawLeft);
		timeline.Append(second);

		timeline.Clock.Next();
		var third = new HeadingTurnAction("c", EHeadingTurn.YawRight);
		timeline.Append(third);

		var drained = timeline.DrainUntil(2);
		Assert.Equal(2, drained.Count);
		Assert.Equal(1, drained[0].Tick);
		Assert.Same(first, Assert.Single(drained[0].Entries));
		Assert.Equal(2, drained[1].Tick);
		Assert.Same(second, Assert.Single(drained[1].Entries));

		Assert.Empty(timeline.History(1));
		Assert.Empty(timeline.History(2));
		Assert.Same(third, Assert.Single(timeline.History(3)));
	}

	[Fact]
	public void ClonePreservesHistoryRecordsAndPending()
	{
		var timeline = new Timeline();
		timeline.Clock.Set(1);
		var action = new HeadingTurnAction("a", EHeadingTurn.YawRight);
		var spawn = new Record<SpawnFacts>(new SpawnFacts(
			"a",
			"t1",
			EType.VoidBomb,
			State.FromShipInstance(ShipInstance.FromCatalog("t1", EType.VoidBomb), Coord.Zero)));
		timeline.Append(action, spawn);
		timeline.Schedule(1, new HeadingTurnAction("b", EHeadingTurn.YawLeft));

		var clone = timeline.Clone();
		Assert.Equal(1, clone.Clock.Current);
		Assert.Equal(2, clone.History(1).Count);
		Assert.Same(action, clone.History(1)[0]);
		Assert.Equal(spawn, clone.History(1)[1]);
		clone.Clock.Next();
		Assert.Single(clone.TakePending());
	}

	[Fact]
	public void PendingContainsActionsOnly()
	{
		var timeline = new Timeline();
		timeline.Clock.Set(1);
		var action = new HeadingTurnAction("a", EHeadingTurn.YawRight);
		timeline.Schedule(0, action);

		Assert.All(timeline.TakePending(1), entry => Assert.IsAssignableFrom<IAction>(entry));
	}

	[Fact]
	public void SimulationEnqueueDoesNotAppendHistory()
	{
		var battle = BattleTestFixture.BeginSimulation(new Coord(5, 5, 5));
		var before = battle.Engine.History().Count;

		Assert.True(battle.PlayerAgent.Sim.TryEnqueue(new MoveStepAction(battle.PlayerId)));

		Assert.Equal(before, battle.Engine.History().Count);
		Assert.Empty(battle.PlayerAgent.Sim.World.Timeline.History());
	}

	[Fact]
	public void CommitAppendsActionThenSpawnRecord()
	{
		var battle = BattleTestFixture.BeginSimulation(new Coord(5, 5, 5));
		battle.Engine.Commit(VoidBombDef.Instance.Bind(battle.PlayerId, ESpatialOrientation.Retro));

		var history = battle.Engine.History();
		var torpedoIndex = history.ToList().FindIndex(entry => entry is VoidBombAction);
		Assert.True(torpedoIndex >= 0);
		var spawn = Assert.IsType<Record<SpawnFacts>>(history[torpedoIndex + 1]);
		Assert.Equal(battle.PlayerId, spawn.Value.SourceId);
		Assert.Equal(EType.VoidBomb, spawn.Value.EntityType);

		var torpedo = Assert.Single(
			UnitRegistry.For(battle.Engine.World).All,
			unit => unit.State.Type == EType.VoidBomb);
		Assert.Equal(torpedo.State.Id, spawn.Value.TargetId);
	}

	[Fact]
	public void CommitAppendsImpactRecordAfterScrapDroneSwarm()
	{
		var origin = new Coord(5, 5, 5);
		var battle = BattleTestFixture.BeginSimulation(origin);
		var action = new ScrapDroneSwarmAction(battle.PlayerId, ESpatialOrientation.Starboard);
		var cells = ScrapDroneSwarmDef.Instance.AffectedCells(action, battle.Engine.World);
		var enemy = UnitRegistry.For(battle.Engine.World).All.First(unit => unit.State.Id != battle.PlayerId);
		enemy.State.Position = cells.First();

		battle.Engine.Commit(action);

		var history = battle.Engine.History();
		var swarmIndex = history.ToList().FindIndex(entry => entry is ScrapDroneSwarmAction);
		Assert.True(swarmIndex >= 0);
		var impact = history
			.Skip(swarmIndex + 1)
			.OfType<Record<ImpactFacts>>()
			.First();
		Assert.Equal(battle.PlayerId, impact.Value.SourceId);
		Assert.Equal(enemy.State.Id, impact.Value.TargetId);
		Assert.Equal(EImpactCause.ScrapDroneSwarmBurst, impact.Value.Cause);
		Assert.True(impact.Value.TotalDamage > 0);
	}

}
