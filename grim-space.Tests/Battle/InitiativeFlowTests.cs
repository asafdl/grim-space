using GrimSpace.Battle;
using GrimSpace.Battle.Ai;
using GrimSpace.Battle.Encounter;
using GrimSpace.Battle.Objectives;
using GrimSpace.Battle.Player;
using GrimSpace.Battle.Units;
using GrimSpace.Math.Grid;
using GrimSpace.Units.Enums;
using GrimSpace.Units.Specs;

namespace GrimSpace.Tests;

[BattleTestSuite]
public sealed class InitiativeFlowTests
{
	[Fact]
	public void ShipSpecs_DefineInitialInitiativeOrder()
	{
		Assert.Equal(40, FighterSpec.Instance.Initiative);
		Assert.Equal(30, RepurposedMinerSpec.Instance.Initiative);
		Assert.Equal(20, GunshipSpec.Instance.Initiative);
		Assert.Equal(20, CarrierSpec.Instance.Initiative);
		Assert.Equal(10, VoidBombSpec.Instance.Initiative);
	}

	[Fact]
	public void ActivationOrder_UsesDescendingInitiativeWithStableTies()
	{
		using var battle = BattleOrchestrator.FromEncounter(
			new BattleEncounter
			{
				Id = "initiative-test",
				Seed = 1,
				Objective = EObjective.EliminateOpponents,
				Spawns =
				[
					Spawn("carrier", EType.Carrier, ETeam.Enemy, 1),
					Spawn("gunship", EType.Gunship, ETeam.Player, 2, new UserExecutionAgent()),
					Spawn("bomb", EType.VoidBomb, ETeam.Enemy, 3),
					Spawn("fighter", EType.Fighter, ETeam.Enemy, 4),
					Spawn("miner", EType.RepurposedMiner, ETeam.Enemy, 5),
				],
			},
			gridSize: 12);

		Assert.Equal(
			["fighter", "miner", "carrier", "gunship", "bomb"],
			UnitRegistry.For(battle.Engine.World).ActivationOrder);
	}

	[Fact]
	public void Preload_OpensPlayerTurnWhenPlayerActsFirst()
	{
		using var battle = BattleOrchestrator.PrepareFromEncounter(
			new BattleEncounter
			{
				Id = "prepare-test",
				Seed = 1,
				Objective = EObjective.EliminateOpponents,
				Spawns =
				[
					Spawn("player", EType.Fighter, ETeam.Player, 1, new UserExecutionAgent()),
					Spawn("enemy", EType.Gunship, ETeam.Enemy, 4),
				],
			},
			gridSize: 12);

		Assert.Equal(EBattlePhase.Preloaded, battle.Phase);

		battle.Preload("test");

		Assert.Equal(EBattlePhase.PlayerTurn, battle.Phase);
	}

	[Fact]
	public void Preload_StaysPreloadedWhenEnemyActsFirst()
	{
		using var battle = BattleOrchestrator.PrepareFromEncounter(
			new BattleEncounter
			{
				Id = "prepare-enemy-first",
				Seed = 1,
				Objective = EObjective.EliminateOpponents,
				Spawns =
				[
					Spawn("player", EType.Gunship, ETeam.Player, 1, new UserExecutionAgent()),
					Spawn("enemy", EType.Fighter, ETeam.Enemy, 4),
				],
			},
			gridSize: 12);

		battle.Preload("test");
		Assert.Equal(EBattlePhase.Preloaded, battle.Phase);

		battle.Start("test");
		Assert.Equal(EBattlePhase.Resolving, battle.Phase);
	}

	private static BattleSpawn Spawn(
		string id,
		EType chassis,
		ETeam team,
		int x,
		GrimSpace.Core.Engine.ExecutionAgent<
			GrimSpace.Battle.World.BattleWorld,
			GrimSpace.Battle.Runtime.ActorRuntime>? agent = null) =>
		BattleSpawnTestKit.Create(
			id,
			chassis,
			team,
			new Coord(x, 1, 1),
			agent ?? new AiController());
}
