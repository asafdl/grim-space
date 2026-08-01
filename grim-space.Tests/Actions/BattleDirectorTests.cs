using GrimSpace.Battle;
using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Movement.Enums;
using GrimSpace.Battle.Presentation;
using GrimSpace.Battle.Presentation.Ui;
using GrimSpace.Math.Grid;
using GrimSpace.Run;
using GrimSpace.Tests.Movement;
using GrimSpace.Units;
using GrimSpace.Units.Enums;

namespace GrimSpace.Tests.Actions;

public sealed class BattleDirectorTests
{
	[Fact]
	public void EndTurnEntersResolvingWithoutWaitingForResolve()
	{
		var origin = new Coord(5, 5, 5);
		var battle = CreateOrchestrator(origin, new Coord(0, 0, 0));
		var option = MovementExpectations.PureForwardMove(origin, stepCount: 3, startMomentum: 0);
		Assert.True(BattleTestActions.TryEnqueueMovePath(battle, option));

		var director = new BattleDirector(new BattleUi(battle));
		director.Start();
		Assert.Equal(PresentationPhase.Planning, director.Phase);

		director.EndTurn();
		Assert.Equal(PresentationPhase.Resolving, director.Phase);
	}

	[Fact]
	public async Task ResolveCompletionRequestsReplay()
	{
		var origin = new Coord(5, 5, 5);
		var battle = CreateOrchestrator(origin, new Coord(0, 0, 0));
		var option = MovementExpectations.PureForwardMove(origin, stepCount: 3, startMomentum: 0);
		Assert.True(BattleTestActions.TryEnqueueMovePath(battle, option));

		var director = new BattleDirector(new BattleUi(battle));
		var replayTcs = new TaskCompletionSource<(TurnReplay Replay, int CompletedTurn)>();
		director.ReplayRequested += (replay, completedTurn) =>
			replayTcs.TrySetResult((replay, completedTurn));
		director.Start();

		director.EndTurn();

		var (replay, completedTurn) = await replayTcs.Task;
		Assert.Equal(PresentationPhase.Replaying, director.Phase);
		Assert.Equal(1, completedTurn);
		Assert.NotEmpty(replay.AppliedActions);
	}

	[Fact]
	public void IncompleteCommitLeavesPlanningPhase()
	{
		var battle = BattleTestFixture.BeginSimulation(new Coord(5, 5, 5));
		var director = new BattleDirector(new BattleUi(battle));
		director.Start();

		Assert.True(battle.Sim.TryEnqueue(new MoveStepAction(battle.PlayerId, ESpatialOrientation.Forward)));
		director.EndTurn();

		Assert.Equal(PresentationPhase.Planning, director.Phase);
	}

	[Fact]
	public void CommandsNoOpOutsidePlanning()
	{
		var origin = new Coord(5, 5, 5);
		var battle = CreateOrchestrator(origin, new Coord(0, 0, 0));
		var option = MovementExpectations.PureForwardMove(origin, stepCount: 3, startMomentum: 0);
		Assert.True(BattleTestActions.TryEnqueueMovePath(battle, option));

		var director = new BattleDirector(new BattleUi(battle));
		director.Start();
		director.EndTurn();

		Assert.False(director.Undo());
		Assert.False(director.SetMode(EPlayerMode.Flak));
		Assert.False(director.Enqueue(new HeadingTurnAction(battle.PlayerId, EHeadingTurn.YawRight)));
	}

	[Fact]
	public async Task NotifyReplayCompleteReturnsToPlanning()
	{
		var origin = new Coord(5, 5, 5);
		var battle = CreateOrchestrator(origin, new Coord(0, 0, 0));
		var option = MovementExpectations.PureForwardMove(origin, stepCount: 3, startMomentum: 0);
		Assert.True(BattleTestActions.TryEnqueueMovePath(battle, option));

		var director = new BattleDirector(new BattleUi(battle));
		var replayTcs = new TaskCompletionSource<TurnReplay>();
		director.ReplayRequested += (replay, _) => replayTcs.TrySetResult(replay);
		director.Start();

		director.EndTurn();
		await replayTcs.Task;

		director.NotifyReplayComplete();
		Assert.Equal(PresentationPhase.Planning, director.Phase);
		Assert.Equal(2, battle.TurnNumber);
	}

	[Fact]
	public void ResolvingFrameDisablesCommands()
	{
		var origin = new Coord(5, 5, 5);
		var battle = CreateOrchestrator(origin, new Coord(0, 0, 0));
		var option = MovementExpectations.PureForwardMove(origin, stepCount: 3, startMomentum: 0);
		Assert.True(BattleTestActions.TryEnqueueMovePath(battle, option));

		var director = new BattleDirector(new BattleUi(battle));
		PresentationFrame? resolvingFrame = null;
		director.FrameChanged += frame =>
		{
			if (director.Phase == PresentationPhase.Resolving)
				resolvingFrame = frame;
		};
		director.Start();

		director.EndTurn();

		Assert.NotNull(resolvingFrame);
		Assert.False(resolvingFrame!.CanAct);
	}

	private static BattleOrchestrator CreateOrchestrator(Coord playerPos, Coord enemyPos)
	{
		var encounter = new Encounter
		{
			Seed = 1,
			Spawns =
			[
				new Spawn
				{
					Unit = new Instance
					{
						Id = "player",
						Type = EType.Fighter,
						Controller = EController.Player,
					},
					Position = playerPos,
				},
				new Spawn
				{
					Unit = new Instance
					{
						Id = "enemy",
						Type = EType.Fighter,
						Controller = EController.Enemy,
					},
					Position = enemyPos,
				},
			],
		};

		return BattleOrchestrator.FromEncounter(encounter, gridSize: 12);
	}
}
