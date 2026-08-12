using GrimSpace.Battle;
using GrimSpace.Battle.Ai;
using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Player;
using GrimSpace.Battle.Movement.Enums;
using GrimSpace.Battle.Presentation;
using GrimSpace.Battle.Presentation.Ui;
using GrimSpace.Math.Grid;
using GrimSpace.Run;
using GrimSpace.Tests.Movement;
using GrimSpace.Units;
using GrimSpace.Units.Enums;

namespace GrimSpace.Tests.Actions;

public sealed class BattlePhaseTests
{
	private const string PlayerId = "player";

	[Fact]
	public void EndTurnEntersResolvingWithoutWaitingForResolve()
	{
		var origin = new Coord(5, 5, 5);
		var battle = CreateOrchestrator(origin, new Coord(0, 0, 0));
		var option = MovementExpectations.PureForwardMove(PlayerId, origin, stepCount: 3, startMomentum: 0);
		Assert.True(BattleTestActions.TryEnqueueMovePath(battle, option));

		Assert.Equal(EBattlePhase.PlayerTurn, battle.Phase);

		battle.EndTurn();
		Assert.Equal(EBattlePhase.Resolving, battle.Phase);
	}

	[Fact]
	public async Task ResolveCompletionRequestsReplay()
	{
		var origin = new Coord(5, 5, 5);
		var battle = CreateOrchestrator(origin, new Coord(0, 0, 0));
		var option = MovementExpectations.PureForwardMove(PlayerId, origin, stepCount: 3, startMomentum: 0);
		Assert.True(BattleTestActions.TryEnqueueMovePath(battle, option));

		var replayTcs = new TaskCompletionSource<(TurnReplay Replay, int CompletedTurn)>();
		battle.TurnResolved += (replay, completedTurn) =>
			replayTcs.TrySetResult((replay, completedTurn));

		battle.EndTurn();

		var (replay, completedTurn) = await replayTcs.Task;
		Assert.Equal(EBattlePhase.Replaying, battle.Phase);
		Assert.Equal(1, completedTurn);
		Assert.NotEmpty(replay.History);
	}

	[Fact]
	public void ShortMoveCommitAdvancesFromPlayerTurn()
	{
		var origin = new Coord(5, 5, 5);
		var battle = BattleTestFixture.BeginSimulation(origin);

		Assert.True(BattleTestCommands.Move(battle, origin + Coord.Forward));
		battle.EndTurn();

		Assert.Equal(EBattlePhase.Resolving, battle.Phase);
	}

	[Fact]
	public void CommandsNoOpOutsidePlayerTurn()
	{
		var origin = new Coord(5, 5, 5);
		var battle = CreateOrchestrator(origin, new Coord(0, 0, 0));
		var option = MovementExpectations.PureForwardMove(PlayerId, origin, stepCount: 3, startMomentum: 0);
		Assert.True(BattleTestActions.TryEnqueueMovePath(battle, option));

		battle.EndTurn();

		Assert.False(battle.AcceptsPlayerInput);
	}

	[Fact]
	public async Task NotifyReplayCompleteReturnsToPlayerTurn()
	{
		var origin = new Coord(5, 5, 5);
		var battle = CreateOrchestrator(origin, new Coord(0, 0, 0));
		var option = MovementExpectations.PureForwardMove(PlayerId, origin, stepCount: 3, startMomentum: 0);
		Assert.True(BattleTestActions.TryEnqueueMovePath(battle, option));

		var replayTcs = new TaskCompletionSource<TurnReplay>();
		battle.TurnResolved += (replay, _) => replayTcs.TrySetResult(replay);

		battle.EndTurn();
		await replayTcs.Task;

		var playerTurnTcs = new TaskCompletionSource();
		battle.PhaseChanged += phase =>
		{
			if (phase == EBattlePhase.PlayerTurn
				&& BattleTestCommands.Frame(battle).MovePaths.Count > 0)
			{
				playerTurnTcs.TrySetResult();
			}
		};

		battle.NotifyReplayComplete();
		await playerTurnTcs.Task;

		Assert.Equal(EBattlePhase.PlayerTurn, battle.Phase);
		Assert.Equal(2, battle.TurnNumber);
	}

	[Fact]
	public void ResolvingFrameDisablesCommands()
	{
		var origin = new Coord(5, 5, 5);
		var battle = CreateOrchestrator(origin, new Coord(0, 0, 0));
		var option = MovementExpectations.PureForwardMove(PlayerId, origin, stepCount: 3, startMomentum: 0);
		Assert.True(BattleTestActions.TryEnqueueMovePath(battle, option));

		PresentationFrame? resolvingFrame = null;
		battle.PhaseChanged += phase =>
		{
			if (phase == EBattlePhase.Resolving)
				resolvingFrame = BattleTestCommands.Frame(battle);
		};
		battle.EndTurn();

		Assert.NotNull(resolvingFrame);
		Assert.False(resolvingFrame!.CanAct);
	}

	[Fact]
	public void EndTurnProceedsRegardlessOfInteractionFocus()
	{
		var origin = new Coord(5, 5, 5);
		var battle = CreateOrchestrator(origin, TurnOrchestrationTests.EnemyInRailgunLine(origin));
		var frames = BattleTestFixture.FrameBuilder(battle);

		var option = MovementExpectations.PureForwardMove(PlayerId, origin, stepCount: 1, startMomentum: 0);
		Assert.True(BattleTestActions.TryEnqueueMovePath(battle, option));

		frames.Interaction.FocusUnit(battle.OpponentId);
		Assert.True(frames.IsInspecting(battle));

		battle.EndTurn();
		Assert.Equal(EBattlePhase.Resolving, battle.Phase);
	}

	[Fact]
	public void ClearFocusRestoresCommands()
	{
		var origin = new Coord(5, 5, 5);
		var battle = CreateOrchestrator(origin, TurnOrchestrationTests.EnemyInRailgunLine(origin));
		var frames = BattleTestFixture.FrameBuilder(battle);

		var option = MovementExpectations.PureForwardMove(PlayerId, origin, stepCount: 1, startMomentum: 0);
		Assert.True(BattleTestActions.TryEnqueueMovePath(battle, option));
		var queuedCount = battle.PlayerAgent.Sim.Actions.Count;

		frames.Interaction.FocusUnit(battle.OpponentId);
		frames.Interaction.ClearFocus();
		Assert.False(frames.IsInspecting(battle));

		frames.Interaction.SetMode(EPlayerMode.Flak);
		Assert.Equal(EPlayerMode.Flak, frames.Interaction.Mode);
		Assert.Equal(queuedCount, battle.PlayerAgent.Sim.Actions.Count);
	}

	[Fact]
	public void FocusUnitRejectsMissingTarget()
	{
		var origin = new Coord(5, 5, 5);
		var battle = CreateOrchestrator(origin, TurnOrchestrationTests.EnemyInRailgunLine(origin));
		var frames = BattleTestFixture.FrameBuilder(battle);

		var previewUnits = frames.BuildFrame(battle, battle.PlayerAgent, acceptsCommands: false).PreviewUnits;
		Assert.False(previewUnits.ContainsKey("missing"));
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
					ExecutionAgent = new UserExecutionAgent(),
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
					ExecutionAgent = new AiController(),
				},
			],
		};

		return BattleOrchestrator.FromEncounter(encounter, gridSize: 12);
	}
}
