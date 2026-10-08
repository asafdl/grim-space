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
