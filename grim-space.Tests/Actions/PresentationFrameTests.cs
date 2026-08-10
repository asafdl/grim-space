using GrimSpace.Battle;
using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Presentation;
using GrimSpace.Battle.Presentation.Domains.Flak;
using GrimSpace.Battle.Presentation.Domains.Railgun;
using GrimSpace.Battle.Presentation.Ui;
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

	[Fact]
	public void DefaultFocusIsPlayer()
	{
		var origin = new Coord(5, 5, 5);
		var battle = CreateOrchestrator(origin, TurnOrchestrationTests.EnemyInRailgunLine(origin));
		var ui = new BattleUi(battle);

		var frame = ui.BuildFrame();

		Assert.Equal(battle.PlayerId, frame.FocusId);
		Assert.False(frame.IsInspecting);
		Assert.True(frame.CanAct);
		Assert.True(frame.ShowMovePreview);
		var focusState = frame.PreviewWorld.StateOf(frame.FocusId);
		Assert.Equal(battle.Sim.StateOf<ActorState>(battle.PlayerId).Position, focusState.Position);
		Assert.All(frame.MovePaths, path => Assert.Equal(battle.PlayerId, path.ActorId));
	}

	[Fact]
	public void FocusEnemyShowsInspectionFrame()
	{
		var origin = new Coord(5, 5, 5);
		var enemyPos = TurnOrchestrationTests.EnemyInRailgunLine(origin);
		var battle = CreateOrchestrator(origin, enemyPos);
		var ui = new BattleUi(battle);

		ui.State.FocusUnit(battle.OpponentId);
		var frame = ui.BuildFrame();

		Assert.Equal(battle.OpponentId, frame.FocusId);
		Assert.True(frame.IsInspecting);
		Assert.False(frame.CanAct);
		Assert.Equal(EPlayerMode.Move, frame.Mode);
		Assert.True(frame.ShowMovePreview);
		Assert.Empty(frame.MovePath);
		Assert.Null(frame.MoveTarget);
		Assert.False(frame.ShowWeaponPreviews);
		Assert.Equal(enemyPos, frame.PreviewWorld.StateOf(frame.FocusId).Position);
		Assert.NotEmpty(frame.MovePaths);
		Assert.All(frame.MovePaths, path => Assert.Equal(battle.OpponentId, path.ActorId));
	}

	[Fact]
	public void InspectionDoesNotMutatePlanning()
	{
		var origin = new Coord(5, 5, 5);
		var battle = CreateOrchestrator(origin, TurnOrchestrationTests.EnemyInRailgunLine(origin));
		var ui = new BattleUi(battle);
		var options = BattleTestFixture.Ui(battle).MoveUi.GetMovePaths(battle.Sim, battle.PlayerId, battle.Sim.Actions).ToList();
		var threeStepIndex = options.FindIndex(
			option => option.EndPosition == origin + Coord.Forward * 3);
		Assert.True(ui.TryQueueMove(threeStepIndex, options));

		var actionsBefore = battle.Sim.Actions.ToList();
		var playerPosBefore = battle.Sim.StateOf<ActorState>(battle.PlayerId).Position;

		ui.State.FocusUnit(battle.OpponentId);
		_ = ui.BuildFrame();

		Assert.Equal(actionsBefore, battle.Sim.Actions);
		Assert.Equal(playerPosBefore, battle.Sim.StateOf<ActorState>(battle.PlayerId).Position);
	}

	[Fact]
	public void InvalidFocusTargetFallsBackToPlayer()
	{
		var origin = new Coord(5, 5, 5);
		var battle = CreateOrchestrator(origin, TurnOrchestrationTests.EnemyInRailgunLine(origin));
		var ui = new BattleUi(battle);

		ui.State.FocusUnit("missing");
		var frame = ui.BuildFrame();

		Assert.Equal(battle.PlayerId, frame.FocusId);
		Assert.False(frame.IsInspecting);
		Assert.Null(ui.State.FocusId);
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
