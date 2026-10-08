using GrimSpace.Battle;
using GrimSpace.Battle.Encounter;
using GrimSpace.Battle.Objectives;
using GrimSpace.Battle.Player;
using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.World;
using GrimSpace.Core.Engine;
using GrimSpace.Math.Grid;
using GrimSpace.Units.Enums;

namespace GrimSpace.Tests;

[BattleTestSuite]
public sealed class MultiShipTurnTests
{
	[Fact]
	public async Task PlayerActivationsReplayWithFollowingAiBeforeNextPlayerTurn()
	{
		using var battle = BattleOrchestrator.FromEncounter(
			new BattleEncounter
			{
				Id = "multi-ship-turn",
				Seed = 1,
				Objective = EObjective.EliminateOpponents,
				Spawns =
				[
					PlayerSpawn("fighter", EType.Fighter, 1),
					AiSpawn("carrier", EType.Carrier, 2),
					PlayerSpawn("gunship", EType.Gunship, 3),
				],
			},
			gridSize: 12);

		Assert.Equal(EBattlePhase.PlayerTurn, battle.Phase);
		Assert.Equal("fighter", battle.ActivePlayerId);
		Assert.True(battle.IsAtRoundStart);

		var firstReplay = NextReplay(battle);
		battle.EndTurn();
		var first = await firstReplay;

		Assert.Equal(["fighter", "carrier"], first.ActivationOrder);
		Assert.Equal(EBattlePhase.Replaying, battle.Phase);
		Assert.Equal(1, battle.TurnNumber);

		battle.NotifyReplayComplete();

		Assert.Equal(EBattlePhase.PlayerTurn, battle.Phase);
		Assert.Equal("gunship", battle.ActivePlayerId);
		Assert.True(battle.PlayerAgent.IsPlanning);
		Assert.False(battle.IsAtRoundStart);

		var secondReplay = NextReplay(battle);
		battle.EndTurn();
		var second = await secondReplay;

		Assert.Equal(["gunship"], second.ActivationOrder);
		Assert.Equal(EBattlePhase.Replaying, battle.Phase);
		Assert.Equal(2, battle.TurnNumber);

		battle.NotifyReplayComplete();

		Assert.Equal(EBattlePhase.PlayerTurn, battle.Phase);
		Assert.Equal("fighter", battle.ActivePlayerId);
		Assert.True(battle.IsAtRoundStart);
	}

	[Fact]
	public void HigherInitiativeAiReplaysBeforeFirstPlayerActivation()
	{
		using var battle = BattleOrchestrator.FromEncounter(
			new BattleEncounter
			{
				Id = "ai-initiative-leader",
				Seed = 1,
				Objective = EObjective.EliminateOpponents,
				Spawns =
				[
					AiSpawn("fighter", EType.Fighter, 1),
					PlayerSpawn("gunship", EType.Gunship, 2),
				],
			},
			gridSize: 12);

		Assert.Equal(EBattlePhase.Replaying, battle.Phase);
		Assert.Equal(["fighter"], battle.PendingReplay!.ActivationOrder);

		battle.NotifyReplayComplete();

		Assert.Equal(EBattlePhase.PlayerTurn, battle.Phase);
		Assert.Equal("gunship", battle.ActivePlayerId);
	}

	private static Task<TurnReplay> NextReplay(BattleOrchestrator battle)
	{
		var ready = new TaskCompletionSource<TurnReplay>(
			TaskCreationOptions.RunContinuationsAsynchronously);
		battle.TurnResolved += OnTurnResolved;
		return ready.Task;

		void OnTurnResolved(TurnReplay replay, int _)
		{
			battle.TurnResolved -= OnTurnResolved;
			ready.TrySetResult(replay);
		}
	}

	private static BattleSpawn PlayerSpawn(string id, EType type, int x) =>
		BattleSpawnTestKit.Create(
			id,
			type,
			ETeam.Player,
			new Coord(x, 1, 1),
			new UserExecutionAgent());

	private static BattleSpawn AiSpawn(string id, EType type, int x) =>
		BattleSpawnTestKit.Create(
			id,
			type,
			ETeam.Enemy,
			new Coord(x, 1, 1),
			new EmptyExecutionAgent());

	private sealed class EmptyExecutionAgent : ExecutionAgent<BattleWorld, ActorRuntime>
	{
		protected override void OnPublishIfReady() => Publish([]);
	}
}
