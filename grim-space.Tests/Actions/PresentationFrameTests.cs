using GrimSpace.Battle;
using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Presentation;
using GrimSpace.Battle.Presentation.Domains.Flak;
using GrimSpace.Battle.Presentation.Domains.Railgun;
using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.Weapons;
using GrimSpace.Math.Grid;
using GrimSpace.Run;
using GrimSpace.Units;
using GrimSpace.Units.Enums;

namespace GrimSpace.Tests.Actions;

public sealed class PresentationFrameTests
{
	[Fact]
	public void FrameAfterQueuedMoveShowsReachableExtensions()
	{
		var origin = new Coord(5, 5, 5);
		var battle = CreateOrchestrator(origin, TurnOrchestrationTests.EnemyInRailgunLine(origin));
		var ui = new BattleUi(battle);
		var options = BattleTestFixture.Ui(battle).MoveUi.GetMovePaths(battle.Sim, battle.PlayerId, battle.Sim.Actions).ToList();
		var threeStepIndex = options.FindIndex(
			option => option.EndPosition == origin + Coord.Forward * 3);

		Assert.True(ui.TryQueueMove(threeStepIndex, options));

		var frame = ui.BuildFrame();
		var endpoints = frame.MovePaths.Select(option => option.EndPosition).ToHashSet();

		Assert.Equal(origin + Coord.Forward * 3, frame.ActorState.Position);
		Assert.Contains(origin + Coord.Forward * 4, endpoints);
		Assert.Equal(origin + Coord.Forward * 3, frame.MoveTarget);
		Assert.Equal(3, frame.MovePath.Count);
	}

	[Fact]
	public void UndoClearsQueuedMoveFromFrame()
	{
		var origin = new Coord(5, 5, 5);
		var battle = CreateOrchestrator(origin, TurnOrchestrationTests.EnemyInRailgunLine(origin));
		var ui = new BattleUi(battle);
		var options = BattleTestFixture.Ui(battle).MoveUi.GetMovePaths(battle.Sim, battle.PlayerId, battle.Sim.Actions).ToList();
		var threeStepIndex = options.FindIndex(
			option => option.EndPosition == origin + Coord.Forward * 3);

		ui.TryQueueMove(threeStepIndex, options);
		Assert.True(ui.Undo());

		var frame = ui.BuildFrame();

		Assert.Equal(origin, frame.ActorState.Position);
		Assert.Null(frame.MoveTarget);
		Assert.Empty(frame.MovePath);
		Assert.Contains(
			frame.MovePaths,
			option => option.EndPosition == origin + Coord.Forward * 4);
	}

	[Fact]
	public void UndoClearsQueuedRailgun()
	{
		var origin = new Coord(5, 5, 5);
		var battle = CreateOrchestrator(origin, TurnOrchestrationTests.EnemyInRailgunLine(origin));
		var ui = new BattleUi(battle);

		Assert.True(RailgunUi.TryApply(battle, ui.State, origin + Coord.Forward));
		Assert.Equal(0, battle.Sim.StateOf<ActorState>(battle.PlayerId).RailgunRemaining);
		Assert.Single(battle.Sim.Actions);

		Assert.True(ui.Undo());

		Assert.Empty(battle.Sim.Actions);
		Assert.Equal(CombatConfig.RailgunsPerTurn, battle.Sim.StateOf<ActorState>(battle.PlayerId).RailgunRemaining);
	}

	[Fact]
	public void UndoClearsQueuedFlak()
	{
		var origin = new Coord(5, 5, 5);
		var battle = CreateOrchestrator(origin, TurnOrchestrationTests.EnemyInRailgunLine(origin));
		var ui = new BattleUi(battle);
		var pickCell = origin + new Coord(1, 1, 0);

		Assert.True(FlakUi.TryApply(battle, ui.State, pickCell));
		Assert.Equal(0, battle.Sim.StateOf<ActorState>(battle.PlayerId).FlakRemaining);

		Assert.True(ui.Undo());

		Assert.Empty(battle.Sim.Actions);
		Assert.Equal(CombatConfig.FlaksPerTurn, battle.Sim.StateOf<ActorState>(battle.PlayerId).FlakRemaining);
	}

	[Fact]
	public void UndoClearsRailgunQueuedAfterMove()
	{
		var origin = new Coord(5, 5, 5);
		var battle = CreateOrchestrator(origin, TurnOrchestrationTests.EnemyInRailgunLine(origin));
		var ui = new BattleUi(battle);
		var options = BattleTestFixture.Ui(battle).MoveUi.GetMovePaths(battle.Sim, battle.PlayerId, battle.Sim.Actions).ToList();
		var threeStepIndex = options.FindIndex(
			option => option.EndPosition == origin + Coord.Forward * 3);

		Assert.True(ui.TryQueueMove(threeStepIndex, options));
		Assert.True(RailgunUi.TryApply(battle, ui.State, origin + Coord.Forward * 4));

		Assert.Equal(4, battle.Sim.Actions.Count);

		Assert.True(ui.Undo());

		Assert.DoesNotContain(battle.Sim.Actions, action => action is RailgunAction);
		Assert.Equal(CombatConfig.RailgunsPerTurn, battle.Sim.StateOf<ActorState>(battle.PlayerId).RailgunRemaining);
		Assert.Equal(3, battle.Sim.Actions.Count(action => action is MoveStepAction));
	}

	[Fact]
	public void UndoRailgunPreservesCommittedMovePath()
	{
		var origin = new Coord(5, 5, 5);
		var battle = CreateOrchestrator(origin, TurnOrchestrationTests.EnemyInRailgunLine(origin));
		var ui = new BattleUi(battle);
		var options = BattleTestFixture.Ui(battle).MoveUi.GetMovePaths(battle.Sim, battle.PlayerId, battle.Sim.Actions).ToList();
		var threeStepIndex = options.FindIndex(
			option => option.EndPosition == origin + Coord.Forward * 3);

		Assert.True(ui.TryQueueMove(threeStepIndex, options));
		var pathAfterMove = ui.State.CommittedMovePath.ToList();
		Assert.NotEmpty(pathAfterMove);

		Assert.True(RailgunUi.TryApply(battle, ui.State, origin + Coord.Forward * 4));
		Assert.True(ui.Undo());

		Assert.Equal(pathAfterMove, ui.State.CommittedMovePath);
		Assert.DoesNotContain(battle.Sim.Actions, action => action is RailgunAction);
		Assert.Equal(CombatConfig.RailgunsPerTurn, battle.Sim.StateOf<ActorState>(battle.PlayerId).RailgunRemaining);
	}

	private static BattleOrchestrator CreateOrchestrator(Coord playerPos, Coord enemyPos)
	{
		var encounter = new Encounter
		{
			Seed = 1,
			Objective = EObjective.EliminateOpponents,
			Spawns =
			[
				new Spawn
				{
					Unit = new Instance
					{
						Id = "player",
						Type = EType.Fighter,
						Alliance = Alliance.Player,
					},
					Position = playerPos,
				},
				new Spawn
				{
					Unit = new Instance
					{
						Id = "enemy",
						Type = EType.Fighter,
						Alliance = Alliance.Enemy,
					},
					Position = enemyPos,
				},
			],
		};

		return BattleOrchestrator.FromEncounter(encounter, gridSize: 12);
	}
}
