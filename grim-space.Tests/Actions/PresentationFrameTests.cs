using GrimSpace.Battle;
using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Player;
using GrimSpace.Battle.Presentation;
using GrimSpace.Battle.Presentation.Ui;
using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.Spatial;
using GrimSpace.Battle.Weapons;
using GrimSpace.Battle.World;
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
		var options = BattleTestCommands.MoveOptions(battle).ToList();
		var threeStepEnd = origin + Coord.Forward * 3;

		Assert.True(BattleTestCommands.Move(battle, threeStepEnd));

		var frame = BattleTestCommands.Frame(battle);
		var endpoints = frame.MovePaths.Select(option => option.EndPosition).ToHashSet();

		Assert.Equal(threeStepEnd, frame.FocusState.Position);
		Assert.Contains(origin + Coord.Forward * 4, endpoints);
		Assert.Equal(threeStepEnd, frame.MoveTarget);
		Assert.Equal(3, frame.MovePath.Count);
	}

	[Fact]
	public void UndoClearsQueuedMoveFromFrame()
	{
		var origin = new Coord(5, 5, 5);
		var battle = CreateOrchestrator(origin, TurnOrchestrationTests.EnemyInRailgunLine(origin));
		var threeStepEnd = origin + Coord.Forward * 3;

		BattleTestCommands.Move(battle, threeStepEnd);
		Assert.True(BattleTestCommands.Undo(battle));

		var frame = BattleTestCommands.Frame(battle);

		Assert.Equal(origin, frame.FocusState.Position);
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

		Assert.True(BattleTestCommands.FireRailgun(battle));
		Assert.Equal(0, battle.Sim.StateOf<ActorState>(battle.PlayerId).RailgunRemaining);
		Assert.Single(battle.Sim.Actions);

		Assert.True(BattleTestCommands.Undo(battle));

		Assert.Empty(battle.Sim.Actions);
		Assert.Equal(CombatConfig.RailgunsPerTurn, battle.Sim.StateOf<ActorState>(battle.PlayerId).RailgunRemaining);
	}

	[Fact]
	public void UndoClearsQueuedFlak()
	{
		var origin = new Coord(5, 5, 5);
		var battle = CreateOrchestrator(origin, TurnOrchestrationTests.EnemyInRailgunLine(origin));
		var pickCell = origin + new Coord(1, 1, 0);

		var frame = BodyFrame.From(battle.Sim.StateOf<ActorState>(battle.PlayerId));
		var mount = WeaponBursts.FlakMountForCell(frame, pickCell);
		Assert.NotNull(mount);
		Assert.True(BattleTestCommands.FireFlak(battle, mount.Value));
		Assert.Equal(0, battle.Sim.StateOf<ActorState>(battle.PlayerId).FlakRemaining);

		Assert.True(BattleTestCommands.Undo(battle));

		Assert.Empty(battle.Sim.Actions);
		Assert.Equal(CombatConfig.FlaksPerTurn, battle.Sim.StateOf<ActorState>(battle.PlayerId).FlakRemaining);
	}

	[Fact]
	public void UndoClearsRailgunQueuedAfterMove()
	{
		var origin = new Coord(5, 5, 5);
		var battle = CreateOrchestrator(origin, TurnOrchestrationTests.EnemyInRailgunLine(origin));
		var threeStepEnd = origin + Coord.Forward * 3;

		Assert.True(BattleTestCommands.Move(battle, threeStepEnd));
		Assert.True(BattleTestCommands.FireRailgun(battle));

		Assert.Equal(4, battle.Sim.Actions.Count);

		Assert.True(BattleTestCommands.Undo(battle));

		Assert.DoesNotContain(battle.Sim.Actions, action => action is RailgunAction);
		Assert.Equal(CombatConfig.RailgunsPerTurn, battle.Sim.StateOf<ActorState>(battle.PlayerId).RailgunRemaining);
		Assert.Equal(3, battle.Sim.Actions.Count(action => action is MoveStepAction));
	}

	[Fact]
	public void UndoRailgunPreservesCommittedMovePath()
	{
		var origin = new Coord(5, 5, 5);
		var battle = CreateOrchestrator(origin, TurnOrchestrationTests.EnemyInRailgunLine(origin));
		var threeStepEnd = origin + Coord.Forward * 3;

		Assert.True(BattleTestCommands.Move(battle, threeStepEnd));
		var pathAfterMove = battle.PlayerAgent.Current.CommittedMovePath.ToList();
		Assert.NotEmpty(pathAfterMove);

		Assert.True(BattleTestCommands.FireRailgun(battle));
		Assert.True(BattleTestCommands.Undo(battle));

		Assert.Equal(pathAfterMove, battle.PlayerAgent.Current.CommittedMovePath);
		Assert.DoesNotContain(battle.Sim.Actions, action => action is RailgunAction);
		Assert.Equal(CombatConfig.RailgunsPerTurn, battle.Sim.StateOf<ActorState>(battle.PlayerId).RailgunRemaining);
	}

	[Fact]
	public void DefaultFocusIsPlayer()
	{
		var origin = new Coord(5, 5, 5);
		var battle = CreateOrchestrator(origin, TurnOrchestrationTests.EnemyInRailgunLine(origin));

		var frame = BattleTestCommands.Frame(battle);

		Assert.Equal(battle.PlayerId, frame.FocusId);
		Assert.False(frame.IsInspecting);
		Assert.True(frame.CanAct);
		Assert.True(frame.ShowMovePreview);
		Assert.Equal(battle.Sim.StateOf<ActorState>(battle.PlayerId).Position, frame.FocusState.Position);
		Assert.NotEmpty(frame.MovePaths);
	}

	[Fact]
	public void FocusEnemyShowsInspectionFrame()
	{
		var origin = new Coord(5, 5, 5);
		var enemyPos = TurnOrchestrationTests.EnemyInRailgunLine(origin);
		var battle = CreateOrchestrator(origin, enemyPos);

		BattleTestCommands.Focus(battle, battle.OpponentId);
		var frame = BattleTestCommands.Frame(battle);

		Assert.Equal(battle.OpponentId, frame.FocusId);
		Assert.True(frame.IsInspecting);
		Assert.False(frame.CanAct);
		Assert.Equal(EPlayerMode.Move, frame.Mode);
		Assert.True(frame.ShowMovePreview);
		Assert.Empty(frame.MovePath);
		Assert.Null(frame.MoveTarget);
		Assert.False(frame.ShowWeaponPreviews);
		Assert.Equal(enemyPos, frame.FocusState.Position);
		Assert.NotEmpty(frame.MovePaths);
	}

	[Fact]
	public void InspectionDoesNotMutatePlanning()
	{
		var origin = new Coord(5, 5, 5);
		var battle = CreateOrchestrator(origin, TurnOrchestrationTests.EnemyInRailgunLine(origin));
		var threeStepEnd = origin + Coord.Forward * 3;
		Assert.True(BattleTestCommands.Move(battle, threeStepEnd));

		var actionsBefore = battle.Sim.Actions.ToList();
		var playerPosBefore = battle.Sim.StateOf<ActorState>(battle.PlayerId).Position;

		BattleTestCommands.Focus(battle, battle.OpponentId);
		_ = BattleTestCommands.Frame(battle);

		Assert.Equal(actionsBefore, battle.Sim.Actions);
		Assert.Equal(playerPosBefore, battle.Sim.StateOf<ActorState>(battle.PlayerId).Position);
	}

	[Fact]
	public void InvalidFocusTargetFallsBackToPlayer()
	{
		var origin = new Coord(5, 5, 5);
		var battle = CreateOrchestrator(origin, TurnOrchestrationTests.EnemyInRailgunLine(origin));

		BattleTestCommands.Focus(battle, "missing");
		var frame = BattleTestCommands.Frame(battle);

		Assert.Equal(battle.PlayerId, frame.FocusId);
		Assert.False(frame.IsInspecting);
		Assert.Null(BattleTestFixture.Ui(battle).State.FocusId);
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
