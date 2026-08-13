using GrimSpace.Battle;
using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Ai;
using GrimSpace.Battle.Units;
using GrimSpace.Math.Grid;
using GrimSpace.Units;
using GrimSpace.Units.Enums;

namespace GrimSpace.Tests.Ai;

public sealed class EnemyRailgunScoringTests
{
	[Fact]
	public async Task BuildTurnActions_FiresRailgunWhenAlignedWithPlayer()
	{
		var playerPos = new Coord(2, 5, 5);
		var enemyPos = new Coord(8, 5, 5);
		var player = CreateUnit(Alliance.Player, "player", playerPos, EType.Fighter, new Coord(1, 0, 0), Coord.Up);
		var enemy = CreateUnit(Alliance.Enemy, "enemy", enemyPos, EType.Carrier, new Coord(-1, 0, 0), Coord.Up);
		enemy.State.ActionPoints = 0;

		var battle = BattleTestFixture.BeginSimulation(player, enemy);
		battle.SetActive(null);
		battle.SetActive(enemy.State.Id);
		var actions = await enemy.ExecutionAgent.GetActions();

		Assert.Contains(actions, action => action is RailgunAction);
	}

	[Fact]
	public async Task BuildTurnActions_DoesNotFireRailgunWhenMisaligned()
	{
		var playerPos = new Coord(2, 5, 5);
		var enemyPos = new Coord(8, 5, 5);
		var player = CreateUnit(Alliance.Player, "player", playerPos, EType.Fighter, Coord.Forward, Coord.Up);
		var enemy = CreateUnit(Alliance.Enemy, "enemy", enemyPos, EType.Carrier, Coord.Forward, Coord.Up);
		enemy.State.ActionPoints = 0;

		var battle = BattleTestFixture.BeginSimulation(player, enemy);
		battle.SetActive(null);
		battle.SetActive(enemy.State.Id);
		var actions = await enemy.ExecutionAgent.GetActions();

		Assert.DoesNotContain(actions, action => action is RailgunAction);
	}

	[Fact]
	public async Task BuildTurnActions_TurnsTowardPlayerWhenParallelAndCannotShoot()
	{
		var playerPos = new Coord(2, 5, 5);
		var enemyPos = new Coord(8, 5, 5);
		var player = CreateUnit(Alliance.Player, "player", playerPos, EType.Fighter, Coord.Forward, Coord.Up);
		var enemy = CreateUnit(Alliance.Enemy, "enemy", enemyPos, EType.Carrier, Coord.Forward, Coord.Up);

		var battle = BattleTestFixture.BeginSimulation(player, enemy);
		battle.SetActive(null);
		battle.SetActive(enemy.State.Id);
		var actions = await enemy.ExecutionAgent.GetActions();

		Assert.Contains(actions, action => action is HeadingTurnAction);
	}

	[Fact]
	public async Task BuildTurnActions_TurnsToFireRailgunWhenShotNeedsAlignment()
	{
		var playerPos = new Coord(2, 5, 5);
		var enemyPos = new Coord(8, 5, 5);
		var player = CreateUnit(Alliance.Player, "player", playerPos, EType.Fighter, new Coord(1, 0, 0), Coord.Up);
		var enemy = CreateUnit(
			Alliance.Enemy,
			"enemy",
			enemyPos,
			EType.Carrier,
			new Coord(0, 0, 1),
			Coord.Up);

		var battle = BattleTestFixture.BeginSimulation(player, enemy);
		battle.SetActive(null);
		battle.SetActive(enemy.State.Id);
		var actions = await enemy.ExecutionAgent.GetActions();

		Assert.Contains(actions, action => action is HeadingTurnAction);
		Assert.Contains(actions, action => action is RailgunAction);
	}

	private static Unit CreateUnit(
		Alliance alliance,
		string id,
		Coord position,
		EType type,
		Coord fore,
		Coord dorsal) =>
		Factory.Create(
			new Instance
			{
				Id = id,
				Type = type,
				Alliance = alliance,
			},
			position,
			new AiController(),
			null,
			0,
			fore,
			dorsal);
}
