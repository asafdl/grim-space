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
	private const string PlayerId = "player";
	[Fact]
	public void EndTurnEntersResolvingWithoutWaitingForResolve()
	{
		var origin = new Coord(5, 5, 5);
		var battle = CreateOrchestrator(origin, new Coord(0, 0, 0));
		var option = MovementExpectations.PureForwardMove(PlayerId, origin, stepCount: 3, startMomentum: 0);
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
		var option = MovementExpectations.PureForwardMove(PlayerId, origin, stepCount: 3, startMomentum: 0);
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
		Assert.NotEmpty(replay.History);
	}

	[Fact]
	public void ShortMoveCommitAdvancesFromPlanning()
	{
		var battle = BattleTestFixture.BeginSimulation(new Coord(5, 5, 5));
		var director = new BattleDirector(new BattleUi(battle));
		director.Start();

		Assert.True(battle.Sim.TryEnqueue(new MoveStepAction(battle.PlayerId, ESpatialOrientation.Forward)));
		director.EndTurn();

		Assert.Equal(PresentationPhase.Resolving, director.Phase);
	}

	[Fact]
	public void CommandsNoOpOutsidePlanning()
	{
		var origin = new Coord(5, 5, 5);
		var battle = CreateOrchestrator(origin, new Coord(0, 0, 0));
		var option = MovementExpectations.PureForwardMove(PlayerId, origin, stepCount: 3, startMomentum: 0);
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
		var option = MovementExpectations.PureForwardMove(PlayerId, origin, stepCount: 3, startMomentum: 0);
		Assert.True(BattleTestActions.TryEnqueueMovePath(battle, option));

		var director = new BattleDirector(new BattleUi(battle));
		var replayTcs = new TaskCompletionSource<TurnReplay>();
		director.ReplayRequested += (replay, _) => replayTcs.TrySetResult(replay);
		director.Start();

		director.EndTurn();
		await replayTcs.Task;

		var planningTcs = new TaskCompletionSource();
		director.FrameChanged += frame =>
		{
			if (director.Phase == PresentationPhase.Planning && frame.MovePaths.Count > 0)
				planningTcs.TrySetResult();
		};

		director.NotifyReplayComplete();
		await planningTcs.Task;

		Assert.Equal(PresentationPhase.Planning, director.Phase);
		Assert.Equal(2, battle.TurnNumber);
	}

	[Fact]
	public void ResolvingFrameDisablesCommands()
	{
		var origin = new Coord(5, 5, 5);
		var battle = CreateOrchestrator(origin, new Coord(0, 0, 0));
		var option = MovementExpectations.PureForwardMove(PlayerId, origin, stepCount: 3, startMomentum: 0);
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

	[Fact]
	public void CommandsRejectedWhileInspecting()
	{
		var origin = new Coord(5, 5, 5);
		var battle = CreateOrchestrator(origin, TurnOrchestrationTests.EnemyInRailgunLine(origin));
		var ui = new BattleUi(battle);
		var director = new BattleDirector(ui);
		director.Start();

		var option = MovementExpectations.PureForwardMove(PlayerId, origin, stepCount: 1, startMomentum: 0);
		Assert.True(BattleTestActions.TryEnqueueMovePath(battle, option));
		var queuedCount = battle.Sim.Actions.Count;

		Assert.True(director.FocusUnit(battle.OpponentId));
		Assert.True(ui.IsInspecting);

		director.EndTurn();
		Assert.Equal(PresentationPhase.Planning, director.Phase);
		Assert.Equal(queuedCount, battle.Sim.Actions.Count);

		Assert.False(director.SetMode(EPlayerMode.Flak));
		Assert.False(director.Enqueue(new HeadingTurnAction(battle.PlayerId, EHeadingTurn.YawRight)));
		Assert.False(director.QueueMove(origin + Coord.Forward * 2));
		Assert.False(director.Undo());
	}

	[Fact]
	public void ClearFocusRestoresCommands()
	{
		var origin = new Coord(5, 5, 5);
		var battle = CreateOrchestrator(origin, TurnOrchestrationTests.EnemyInRailgunLine(origin));
		var ui = new BattleUi(battle);
		var director = new BattleDirector(ui);
		director.Start();

		var option = MovementExpectations.PureForwardMove(PlayerId, origin, stepCount: 1, startMomentum: 0);
		Assert.True(BattleTestActions.TryEnqueueMovePath(battle, option));
		var queuedCount = battle.Sim.Actions.Count;

		Assert.True(director.FocusUnit(battle.OpponentId));
		Assert.True(director.ClearFocus());
		Assert.False(ui.IsInspecting);
		Assert.True(director.SetMode(EPlayerMode.Flak));
		Assert.Equal(queuedCount, battle.Sim.Actions.Count);
	}

	[Fact]
	public void FocusUnitRejectsMissingTarget()
	{
		var origin = new Coord(5, 5, 5);
		var battle = CreateOrchestrator(origin, TurnOrchestrationTests.EnemyInRailgunLine(origin));
		var director = new BattleDirector(new BattleUi(battle));
		director.Start();

		Assert.False(director.FocusUnit("missing"));
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
