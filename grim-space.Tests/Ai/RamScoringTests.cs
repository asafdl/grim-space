using GrimSpace.Battle;
using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Ai;
using GrimSpace.Battle.Units;
using GrimSpace.Math.Grid;
using GrimSpace.Units;
using GrimSpace.Units.Enums;

namespace GrimSpace.Tests.Ai;

[BattleTestSuite]
public sealed class RamScoringTests
{
	[Fact]
	public async Task BuildTurnActions_RamsWhenAlignedWithPlayerInRange()
	{
		var playerPos = new Coord(5, 5, 5);
		var enemyPos = new Coord(5, 5, 2);
		var player = CreateUnit(ETeam.Player, "player", playerPos, EType.Fighter, Coord.Forward, Coord.Up);
		var enemy = CreateUnit(
			ETeam.Enemy,
			"gooper",
			enemyPos,
			EType.IndustrialGooper,
			Coord.Forward,
			Coord.Up);
		enemy.State.ActionPoints = 0;

		var battle = BattleTestFixture.BeginSimulation(player, enemy);
		var actions = await BattleTestFixture.AwaitUnitActions(battle, enemy);

		Assert.Contains(actions, action => action is RamAction);
	}

	[Fact]
	public async Task BuildTurnActions_TurnsAndRamsWhenTranslationApIsExhausted()
	{
		var playerPos = new Coord(5, 5, 5);
		var enemyPos = new Coord(5, 5, 2);
		var player = CreateUnit(ETeam.Player, "player", playerPos, EType.Fighter, Coord.Forward, Coord.Up);
		var enemy = CreateUnit(
			ETeam.Enemy,
			"gooper",
			enemyPos,
			EType.IndustrialGooper,
			new Coord(1, 0, 0),
			Coord.Up);
		enemy.State.ActionPoints = 0;

		var battle = BattleTestFixture.BeginSimulation(player, enemy);
		var actions = await BattleTestFixture.AwaitUnitActions(battle, enemy);

		Assert.Contains(actions, action => action is HeadingTurnAction);
		Assert.Contains(actions, action => action is RamAction);
	}

	[Fact]
	public async Task BuildTurnActions_ApproachesWhenPlayerIsOutOfRamRange()
	{
		var playerPos = new Coord(5, 5, 5);
		var enemyPos = new Coord(5, 5, 0);
		var player = CreateUnit(ETeam.Player, "player", playerPos, EType.Fighter, Coord.Forward, Coord.Up);
		var enemy = CreateUnit(
			ETeam.Enemy,
			"gooper",
			enemyPos,
			EType.IndustrialGooper,
			Coord.Forward,
			Coord.Up);
		enemy.State.ActionPoints = 1;

		var battle = BattleTestFixture.BeginSimulation(player, enemy);
		var actions = await BattleTestFixture.AwaitUnitActions(battle, enemy);

		Assert.Contains(actions, action => action is MoveStepAction);
		Assert.DoesNotContain(actions, action => action is RamAction);
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
