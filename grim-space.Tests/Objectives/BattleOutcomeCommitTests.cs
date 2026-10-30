using GrimSpace.Battle;
using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Ai;
using GrimSpace.Battle.Encounter;
using GrimSpace.Battle.Objectives;
using GrimSpace.Battle.Player;
using GrimSpace.Core.Actions;
using GrimSpace.Core.Engine;
using GrimSpace.Math.Grid;
using GrimSpace.Tests;
using GrimSpace.Tests.Actions;
using GrimSpace.Battle.Units;
using GrimSpace.Units.Enums;

namespace GrimSpace.Tests.Objectives;

[IntegrationTestSuite]
public sealed class BattleOutcomeCommitTests
{
	[Fact]
	public void OngoingEvaluation_EmitsNoOutcomeRecord()
	{
		using var battle = TurnOrchestrationTests.CreateOrchestrator(new Coord(5, 5, 5), new Coord(0, 0, 0));
		var notifications = 0;
		using var subscription = battle.Subscribe<Record<BattleOutcome>>(_ => notifications++);

		BattleTestActions.CommitAndResolve(battle);

		Assert.Equal(EBattleResult.Ongoing, battle.Engine.World.battleResult);
		Assert.Equal(0, notifications);
		Assert.DoesNotContain(
			battle.Engine.World.Timeline.History(),
			entry => entry is Record<BattleOutcome>);
	}

	[Fact]
	public void TerminalEvaluation_EmitsOneCommittedRecord()
	{
		using var battle = TurnOrchestrationTests.CreateOrchestrator(new Coord(5, 5, 5), new Coord(0, 0, 0));
		var enemyId = BattleTestFixture.FirstEnemyId(battle);
		battle.Engine.World.StateOf(enemyId).HullPoints = 0;
		BattleTestFixture.ResetPlayerPlanning(battle);

		Record<BattleOutcome>? received = null;
		using var subscription = battle.Subscribe<Record<BattleOutcome>>(record => received = record);

		BattleTestActions.CommitAndResolve(battle);

		Assert.Equal(EBattleResult.Win, battle.Engine.World.battleResult);
		Assert.NotNull(received);
		Assert.Equal(EBattleResult.Win, received!.Value.Result);
		Assert.Equal(0, battle.Engine.World.StateOf(enemyId).HullPoints);
	}

	[Fact]
	public void Retire_EmitsLossWithoutDestroyingUnits()
	{
		using var battle = TurnOrchestrationTests.CreateOrchestrator(new Coord(5, 5, 5), new Coord(0, 0, 0));
		var enemyId = BattleTestFixture.FirstEnemyId(battle);
		var enemyHp = battle.Engine.World.StateOf(enemyId).HullPoints;

		Record<BattleOutcome>? received = null;
		using var subscription = battle.Subscribe<Record<BattleOutcome>>(record => received = record);

		battle.Retire();

		Assert.Equal(EBattleResult.Lose, battle.Engine.World.battleResult);
		Assert.NotNull(received);
		Assert.Equal(enemyHp, battle.Engine.World.StateOf(enemyId).HullPoints);
	}

	[Fact]
	public void Draw_IsCommittedAsLoss()
	{
		using var battle = TurnOrchestrationTests.CreateOrchestrator(new Coord(5, 5, 5), new Coord(0, 0, 0));
		foreach (var unit in UnitRegistry.For(battle.Engine.World).All)
			unit.State.HullPoints = 0;
		BattleTestFixture.ResetPlayerPlanning(battle);

		BattleTestActions.CommitAndResolve(battle);

		Assert.Equal(EBattleResult.Lose, battle.Engine.World.battleResult);
	}

	[Fact]
	public async Task EliminatingLastOpponentEndsBattleWithoutRemainingActivations()
	{
		using var battle = BattleOrchestrator.FromEncounter(
			new BattleEncounter
			{
				Id = "early-win",
				Seed = 1,
				Objective = EObjective.EliminateOpponents,
				Spawns =
				[
					BattleSpawnTestKit.Create(
						"lead",
						EType.Fighter,
						ETeam.Player,
						new Coord(1, 1, 1),
						new UserExecutionAgent()),
					BattleSpawnTestKit.Create(
						"wing",
						EType.Gunship,
						ETeam.Player,
						new Coord(2, 1, 1),
						new UserExecutionAgent()),
					BattleSpawnTestKit.Create(
						"enemy",
						EType.Fighter,
						ETeam.Enemy,
						new Coord(8, 1, 1),
						new AiController()),
				],
			},
			gridSize: 12,
			preferredPlayerId: "lead");

		var enemyId = BattleTestFixture.FirstEnemyId(battle);
		battle.Engine.World.StateOf(enemyId).HullPoints = 0;
		BattleTestFixture.ResetPlayerPlanning(battle);

		var replayReady = new TaskCompletionSource<TurnReplay>(
			TaskCreationOptions.RunContinuationsAsynchronously);
		void OnTurnResolved(TurnReplay replay, int _) => replayReady.TrySetResult(replay);
		battle.TurnResolved += OnTurnResolved;
		battle.EndTurn();
		var replay = await replayReady.Task;
		battle.TurnResolved -= OnTurnResolved;

		Assert.Equal(["lead"], replay.ActivationOrder);
		Assert.Equal(EBattleResult.Win, battle.Engine.World.battleResult);

		battle.NotifyReplayComplete();

		Assert.Equal(EBattlePhase.BattleOver, battle.Phase);
		Assert.Equal("lead", battle.ActivePlayerId);
	}

	[Fact]
	public void SimulationPeek_DoesNotNotifyOutcomeSubscribers()
	{
		using var battle = TurnOrchestrationTests.CreateOrchestrator(new Coord(5, 5, 5), new Coord(0, 0, 0));
		var notifications = 0;
		using var subscription = battle.Subscribe<Record<BattleOutcome>>(_ => notifications++);

		var sim = battle.Engine.CreateSimulation();
		var action = CommitBattleOutcomeDef.Instance.BindEvaluate();
		sim.Peek(action);

		Assert.Equal(0, notifications);
	}
}
