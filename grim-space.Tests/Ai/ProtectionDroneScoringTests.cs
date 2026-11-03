using GrimSpace.Battle;
using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Ai;
using GrimSpace.Battle.Units;
using GrimSpace.Math.Grid;
using GrimSpace.Units;
using GrimSpace.Units.Enums;

namespace GrimSpace.Tests.Ai;

[BattleTestSuite]
public sealed class ProtectionDroneScoringTests
{
	[Fact]
	public async Task BuildTurnActions_CastsProtectionDroneOnThreatenedAllyBeforeOtherActions()
	{
		var playerPos = new Coord(3, 5, 5);
		var allyPos = new Coord(8, 5, 5);
		var minerPos = new Coord(8, 6, 5);
		var player = CreateUnit(ETeam.Player, "player", playerPos, EType.Fighter, new Coord(1, 0, 0), Coord.Up);
		var ally = CreateUnit(ETeam.Enemy, "ally", allyPos, EType.Fighter, new Coord(-1, 0, 0), Coord.Up);
		ally.State.ActionPoints = 0;
		var miner = CreateUnit(
			ETeam.Enemy,
			"miner",
			minerPos,
			EType.RepurposedMiner,
			new Coord(1, 0, 0),
			Coord.Up);
		miner.State.ActionPoints = 0;

		var battle = BattleTestFixture.BeginSimulation(player, [ally, miner]);
		var actions = await BattleTestFixture.AwaitUnitActions(battle, miner);

		var drone = Assert.IsType<ProtectionDroneAction>(
			Assert.Single(actions, action => action is ProtectionDroneAction));
		Assert.Equal(ally.State.Id, drone.TargetId);
		Assert.Equal(0, actions.ToList().IndexOf(drone));
	}

	[Fact]
	public async Task BuildTurnActions_SkipsProtectionDroneWhenNoAllyIsThreatened()
	{
		var playerPos = new Coord(0, 0, 0);
		var allyPos = new Coord(8, 5, 5);
		var minerPos = new Coord(8, 6, 5);
		var player = CreateUnit(ETeam.Player, "player", playerPos, EType.Fighter, Coord.Forward, Coord.Up);
		var ally = CreateUnit(ETeam.Enemy, "ally", allyPos, EType.Fighter, new Coord(-1, 0, 0), Coord.Up);
		ally.State.ActionPoints = 0;
		var miner = CreateUnit(
			ETeam.Enemy,
			"miner",
			minerPos,
			EType.RepurposedMiner,
			new Coord(1, 0, 0),
			Coord.Up);
		miner.State.ActionPoints = 0;

		var battle = BattleTestFixture.BeginSimulation(player, [ally, miner]);
		var actions = await BattleTestFixture.AwaitUnitActions(battle, miner);

		Assert.DoesNotContain(actions, action => action is ProtectionDroneAction);
	}

	[Fact]
	public void SearchCapabilities_ExcludesProtectionDrone()
	{
		var miner = CreateUnit(
			ETeam.Enemy,
			"miner",
			new Coord(5, 5, 5),
			EType.RepurposedMiner,
			Coord.Forward,
			Coord.Up);
		var battle = BattleTestFixture.BeginSimulation(
			CreateUnit(ETeam.Player, "player", new Coord(0, 0, 0), EType.Fighter, Coord.Forward, Coord.Up),
			miner);
		var runtime = battle.Engine.ActorRuntimes.For(miner.State.Id);

		Assert.DoesNotContain(
			AiController.SearchCapabilities(miner.State, runtime),
			def => def is ProtectionDroneDef);
	}

	[Fact]
	public void BestProtectionDroneAction_PicksMostThreatenedAlly()
	{
		var playerPos = new Coord(3, 5, 5);
		var threatenedPos = new Coord(8, 5, 5);
		var safePos = new Coord(8, 8, 8);
		var minerPos = new Coord(8, 6, 5);
		var player = CreateUnit(ETeam.Player, "player", playerPos, EType.Fighter, new Coord(1, 0, 0), Coord.Up);
		var threatened = CreateUnit(ETeam.Enemy, "threatened", threatenedPos, EType.Fighter, Coord.Forward, Coord.Up);
		var safe = CreateUnit(ETeam.Enemy, "safe", safePos, EType.Fighter, Coord.Forward, Coord.Up);
		var miner = CreateUnit(
			ETeam.Enemy,
			"miner",
			minerPos,
			EType.RepurposedMiner,
			Coord.Forward,
			Coord.Up);
		var battle = BattleTestFixture.BeginSimulation(player, [threatened, safe, miner]);
		var runtime = battle.Engine.ActorRuntimes.For(miner.State.Id);

		var action = EnemySearchInput.BestProtectionDroneAction(
			battle.Engine.World,
			runtime,
			miner.State.Id);

		Assert.NotNull(action);
		Assert.Equal(threatened.State.Id, action!.TargetId);
	}

	private static Unit CreateUnit(
		ETeam team,
		string id,
		Coord position,
		EType type,
		Coord fore,
		Coord dorsal) =>
		Factory.Create(
			ShipInstance.FromCatalog(id, type),
			team,
			position,
			new AiController(),
			fore,
			dorsal);
}
