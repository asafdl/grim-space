using GrimSpace.Battle;
using GrimSpace.Battle.Ai;
using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Player;
using GrimSpace.Battle.Presentation;
using GrimSpace.Battle.Presentation.Interaction;
using GrimSpace.Battle.Presentation.Ui;
using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.Spatial;
using GrimSpace.Battle.Abilities;
using GrimSpace.Battle.World;
using GrimSpace.Math.Grid;
using GrimSpace.Battle.Encounter;
using GrimSpace.Battle.Objectives;
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
		Assert.Equal(0, battle.PlayerAgent.Sim.StateOf<ActorState>(battle.PlayerId).RailgunRemaining);
		Assert.Single(battle.PlayerAgent.Sim.Actions);

		Assert.True(BattleTestCommands.Undo(battle));

		Assert.Empty(battle.PlayerAgent.Sim.Actions);
		Assert.Equal(CombatConfig.RailgunsPerTurn, battle.PlayerAgent.Sim.StateOf<ActorState>(battle.PlayerId).RailgunRemaining);
	}

	[Fact]
	public void UndoClearsQueuedFlak()
	{
		var origin = new Coord(5, 5, 5);
		var battle = CreateOrchestrator(origin, TurnOrchestrationTests.EnemyInRailgunLine(origin));
		var pickCell = origin + new Coord(1, 1, 0);

		var frame = BodyFrame.From(battle.PlayerAgent.Sim.StateOf<ActorState>(battle.PlayerId));
		var mountedOn = WeaponBursts.FlakMountedOnForCell(frame, pickCell);
		Assert.NotNull(mountedOn);
		Assert.True(BattleTestCommands.FireFlak(battle, mountedOn.Value));
		Assert.Equal(0, battle.PlayerAgent.Sim.StateOf<ActorState>(battle.PlayerId).FlakRemaining);

		Assert.True(BattleTestCommands.Undo(battle));

		Assert.Empty(battle.PlayerAgent.Sim.Actions);
		Assert.Equal(CombatConfig.FlaksPerTurn, battle.PlayerAgent.Sim.StateOf<ActorState>(battle.PlayerId).FlakRemaining);
	}

	[Fact]
	public void UndoClearsRailgunQueuedAfterMove()
	{
		var origin = new Coord(5, 5, 5);
		var battle = CreateOrchestrator(origin, TurnOrchestrationTests.EnemyInRailgunLine(origin));
		var threeStepEnd = origin + Coord.Forward * 3;

		Assert.True(BattleTestCommands.Move(battle, threeStepEnd));
		Assert.True(BattleTestCommands.FireRailgun(battle));

		Assert.Equal(4, battle.PlayerAgent.Sim.Actions.Count);

		Assert.True(BattleTestCommands.Undo(battle));

		Assert.DoesNotContain(battle.PlayerAgent.Sim.Actions, action => action is RailgunAction);
		Assert.Equal(CombatConfig.RailgunsPerTurn, battle.PlayerAgent.Sim.StateOf<ActorState>(battle.PlayerId).RailgunRemaining);
		Assert.Equal(3, battle.PlayerAgent.Sim.Actions.Count(action => action is MoveStepAction));
	}

	[Fact]
	public void UndoRailgunPreservesCommittedMovePath()
	{
		var origin = new Coord(5, 5, 5);
		var battle = CreateOrchestrator(origin, TurnOrchestrationTests.EnemyInRailgunLine(origin));
		var threeStepEnd = origin + Coord.Forward * 3;

		Assert.True(BattleTestCommands.Move(battle, threeStepEnd));
		var preview = new PlanningPreview();
		var pathAfterMove = preview.CommittedMovePath(battle.PlayerAgent.Sim, battle.PlayerId).ToList();
		Assert.NotEmpty(pathAfterMove);

		Assert.True(BattleTestCommands.FireRailgun(battle));
		Assert.True(BattleTestCommands.Undo(battle));

		Assert.Equal(pathAfterMove, preview.CommittedMovePath(battle.PlayerAgent.Sim, battle.PlayerId));
		Assert.DoesNotContain(battle.PlayerAgent.Sim.Actions, action => action is RailgunAction);
		Assert.Equal(CombatConfig.RailgunsPerTurn, battle.PlayerAgent.Sim.StateOf<ActorState>(battle.PlayerId).RailgunRemaining);
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
		Assert.Equal(battle.PlayerAgent.Sim.StateOf<ActorState>(battle.PlayerId).Position, frame.FocusState.Position);
		Assert.NotEmpty(frame.MovePaths);
	}

	[Fact]
	public void FocusEnemyShowsInspectionFrame()
	{
		var origin = new Coord(5, 5, 5);
		var enemyPos = TurnOrchestrationTests.EnemyInRailgunLine(origin);
		var battle = CreateOrchestrator(origin, enemyPos);

		BattleTestCommands.Focus(battle, BattleTestFixture.FirstEnemyId(battle));
		var frame = BattleTestCommands.Frame(battle);

		Assert.Equal(BattleTestFixture.FirstEnemyId(battle), frame.FocusId);
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

		var actionsBefore = battle.PlayerAgent.Sim.Actions.ToList();
		var playerPosBefore = battle.PlayerAgent.Sim.StateOf<ActorState>(battle.PlayerId).Position;

		BattleTestCommands.Focus(battle, BattleTestFixture.FirstEnemyId(battle));
		_ = BattleTestCommands.Frame(battle);

		Assert.Equal(actionsBefore, battle.PlayerAgent.Sim.Actions);
		Assert.Equal(playerPosBefore, battle.PlayerAgent.Sim.StateOf<ActorState>(battle.PlayerId).Position);
	}

	[Fact]
	public void QueuedWeaponsRetainTheirOwnActorStateAtQueueIndex()
	{
		var origin = new Coord(5, 5, 5);
		var battle = CreateOrchestrator(origin, TurnOrchestrationTests.EnemyInRailgunLine(origin));
		var afterMove = origin + Coord.Forward * 2;

		Assert.True(BattleTestCommands.FireRailgun(battle));
		Assert.True(BattleTestCommands.Move(battle, afterMove));
		Assert.True(BattleTestCommands.FireFlak(battle, ESpatialOrientation.Port));

		var preview = new PlanningPreview();
		var queued = preview.QueuedWeapon(battle.PlayerAgent.Sim, battle.PlayerId);

		Assert.True(queued.Railgun);
		Assert.Equal(ESpatialOrientation.Port, queued.FlakMountedOn);
		Assert.NotNull(queued.RailgunActorStateAtQueue);
		Assert.NotNull(queued.FlakActorStateAtQueue);
		Assert.Equal(origin, queued.RailgunActorStateAtQueue.Position);
		Assert.Equal(afterMove, queued.FlakActorStateAtQueue.Position);
		Assert.Equal(
			afterMove,
			preview.PreviewUnits(battle.PlayerAgent.Sim, battle.PlayerId)[battle.PlayerId].Position);
	}

	[Fact]
	public void ThreatenedUnitIdsIncludesTargetsFromEveryQueuedWeapon()
	{
		var origin = new Coord(5, 5, 5);
		var battle = CreateOrchestrator(origin, TurnOrchestrationTests.EnemyInRailgunLine(origin));
		var enemyId = BattleTestFixture.FirstEnemyId(battle);
		var preview = new PlanningPreview();

		Assert.True(BattleTestCommands.FireRailgun(battle));
		Assert.Contains(
			enemyId,
			preview.ThreatenedUnitIds(battle.PlayerAgent.Sim, battle.PlayerId, new InteractionState()));

		Assert.True(BattleTestCommands.FireFlak(battle, ESpatialOrientation.Port));

		Assert.Contains(
			enemyId,
			preview.ThreatenedUnitIds(battle.PlayerAgent.Sim, battle.PlayerId, new InteractionState()));
	}

	[Fact]
	public void PreviewRetainsPredictedDeadUnits()
	{
		var origin = new Coord(5, 5, 5);
		var battle = CreateOrchestrator(origin, TurnOrchestrationTests.EnemyInRailgunLine(origin));
		var enemyId = BattleTestFixture.FirstEnemyId(battle);
		var enemy = battle.PlayerAgent.Sim.StateOf<ActorState>(enemyId);
		enemy.HullPoints = 1;
		foreach (var face in Enum.GetValues<ESpatialOrientation>())
			enemy.ShieldPoints[face] = 0;

		Assert.True(BattleTestCommands.FireRailgun(battle));

		var preview = new PlanningPreview().PreviewUnits(battle.PlayerAgent.Sim, battle.PlayerId);

		Assert.True(preview.ContainsKey(enemyId));
		Assert.False(preview[enemyId].IsAlive);
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
		Assert.Null(BattleTestFixture.FrameBuilder(battle).Interaction.FocusId);
	}

	private static BattleOrchestrator CreateOrchestrator(Coord playerPos, Coord enemyPos)
	{
		var encounter = new BattleEncounter
		{
			Seed = 1,
			Objective = EObjective.EliminateOpponents,
			Spawns =
			[
				new BattleSpawn
				{
					Unit = new Instance
					{
						Id = "player",
						Type = EType.Fighter,
						Alliance = Alliance.Player,
					},
					Position = playerPos,
					ExecutionAgent = new UserExecutionAgent(),
				},
				new BattleSpawn
				{
					Unit = new Instance
					{
						Id = "enemy",
						Type = EType.Fighter,
						Alliance = Alliance.Enemy,
					},
					Position = enemyPos,
					ExecutionAgent = new AiController(),
				},
			],
		};

		return BattleOrchestrator.FromEncounter(encounter, gridSize: 12);
	}
}
