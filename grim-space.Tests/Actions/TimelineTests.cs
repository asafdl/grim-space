using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Effects;
using GrimSpace.Battle.Movement.Enums;
using GrimSpace.Battle.Spatial;
using GrimSpace.Battle.Units;
using GrimSpace.Battle.Abilities;
using GrimSpace.Battle.World;
using GrimSpace.Core.Actions;
using GrimSpace.Core.Engine;
using GrimSpace.Math.Grid;
using GrimSpace.Units.Enums;

namespace GrimSpace.Tests.Actions;

public sealed class TimelineTests
{
	[Fact]
	public void CommitRecordsHistoryByActor()
	{
		var battle = BattleTestFixture.BeginSimulation(new Coord(5, 5, 5));
		var tick = battle.TurnNumber;
		Assert.True(battle.PlayerAgent.Sim.TryEnqueue(new HeadingTurnAction(battle.PlayerId, EHeadingTurn.YawRight)));
		BattleTestActions.CommitAndResolve(battle);

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
	public void ClonePreservesHistoryRecordsAndPending()
	{
		var timeline = new Timeline();
		timeline.Clock.Set(1);
		var action = new HeadingTurnAction("a", EHeadingTurn.YawRight);
		var spawn = new Record<SpawnFacts>(new SpawnFacts("a", "t1", EType.Torpedo));
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

		Assert.True(battle.PlayerAgent.Sim.TryEnqueue(new HeadingTurnAction(battle.PlayerId, EHeadingTurn.YawRight)));

		Assert.Equal(before, battle.Engine.History().Count);
		Assert.Empty(battle.PlayerAgent.Sim.World.Timeline.History());
	}

	[Fact]
	public void CommitAppendsActionThenSpawnRecord()
	{
		var battle = BattleTestFixture.BeginSimulation(new Coord(5, 5, 5));
		battle.Engine.Commit(new TorpedoAction(battle.PlayerId, ETorpedoMount.Aft));

		var history = battle.Engine.History();
		var torpedoIndex = history.ToList().FindIndex(entry => entry is TorpedoAction);
		Assert.True(torpedoIndex >= 0);
		var spawn = Assert.IsType<Record<SpawnFacts>>(history[torpedoIndex + 1]);
		Assert.Equal(battle.PlayerId, spawn.Value.SourceId);
		Assert.Equal(EType.Torpedo, spawn.Value.EntityType);

		var torpedo = Assert.Single(
			UnitRegistry.For(battle.Engine.World).All,
			unit => unit.State.Type == EType.Torpedo);
		Assert.Equal(torpedo.State.Id, spawn.Value.TargetId);
	}

	[Fact]
	public void CommitAppendsImpactRecordAfterFlak()
	{
		var origin = new Coord(5, 5, 5);
		var battle = BattleTestFixture.BeginSimulation(origin, momentum: 1);
		var frame = BodyFrame.From(battle.Engine.World.StateOf(battle.PlayerId));
		var cells = WeaponBursts.FlakBurstCells(
			frame,
			FlakMountConfig.For(EFlakMount.Starboard),
			battle.Engine.World.Grid.IsInBounds);
		var enemy = UnitRegistry.For(battle.Engine.World).All.First(unit => unit.State.Id != battle.PlayerId);
		enemy.State.Position = cells.First();

		battle.Engine.Commit(new FlakAction(battle.PlayerId, EFlakMount.Starboard));

		var history = battle.Engine.History();
		var flakIndex = history.ToList().FindIndex(entry => entry is FlakAction);
		Assert.True(flakIndex >= 0);
		var impact = Assert.IsType<Record<ImpactFacts>>(history[flakIndex + 1]);
		Assert.Equal(battle.PlayerId, impact.Value.SourceId);
		Assert.Equal(enemy.State.Id, impact.Value.TargetId);
		Assert.Equal(EHazardKind.FlakBurst, impact.Value.Cause);
		Assert.True(impact.Value.ShieldDamage + impact.Value.HullDamage + impact.Value.MomentumLoss > 0);
	}
}
