using GrimSpace.Battle;
using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Ai;
using GrimSpace.Battle.Units;
using GrimSpace.Battle.World;
using GrimSpace.Math.Grid;
using GrimSpace.Units;
using GrimSpace.Units.Enums;

namespace GrimSpace.Tests.Ai;

[BattleTestSuite]
public sealed class GoopGunScoringTests
{
	[Fact]
	public void BlockingOneThreatScoresHigherThanOneDamageHit()
	{
		Assert.True(EnemySearchInput.GoopThreatBlockBonus > EnemySearchInput.DamageHitBonus);
	}

	[Fact]
	public async Task BuildTurnActions_FiresGoopGunWhenItBlocksOpponentLightning()
	{
		var playerPos = new Coord(2, 5, 5);
		var enemyPos = new Coord(8, 5, 5);
		var player = CreateUnit(ETeam.Player, "player", playerPos, EType.Fighter, new Coord(1, 0, 0), Coord.Up);
		var enemy = CreateUnit(
			ETeam.Enemy,
			"gooper",
			enemyPos,
			EType.IndustrialGooper,
			new Coord(-1, 0, 0),
			Coord.Up);
		enemy.State.ActionPoints = 0;

		var battle = BattleTestFixture.BeginSimulation(player, enemy);
		var actions = await BattleTestFixture.AwaitUnitActions(battle, enemy);

		Assert.Contains(actions, action => action is GoopGunAction);
	}

	[Fact]
	public async Task BuildTurnActions_DoesNotFireGoopGunWhenOpponentCannotThreaten()
	{
		var playerPos = new Coord(2, 5, 5);
		var enemyPos = new Coord(8, 5, 5);
		var player = CreateUnit(ETeam.Player, "player", playerPos, EType.Fighter, Coord.Forward, Coord.Up);
		var enemy = CreateUnit(
			ETeam.Enemy,
			"gooper",
			enemyPos,
			EType.IndustrialGooper,
			new Coord(-1, 0, 0),
			Coord.Up);
		enemy.State.ActionPoints = 0;

		var battle = BattleTestFixture.BeginSimulation(player, enemy);
		var actions = await BattleTestFixture.AwaitUnitActions(battle, enemy);

		Assert.DoesNotContain(actions, action => action is GoopGunAction);
	}

	[Fact]
	public async Task BuildTurnActions_FiresGoopGunWhenItBlocksLightningThreateningAnAlly()
	{
		var playerPos = new Coord(2, 5, 5);
		var allyPos = new Coord(8, 5, 5);
		var gooperPos = new Coord(8, 6, 5);
		var player = CreateUnit(ETeam.Player, "player", playerPos, EType.Fighter, new Coord(1, 0, 0), Coord.Up);
		var ally = CreateUnit(ETeam.Enemy, "ally", allyPos, EType.Fighter, new Coord(-1, 0, 0), Coord.Up);
		ally.State.ActionPoints = 0;
		var gooper = CreateUnit(
			ETeam.Enemy,
			"gooper",
			gooperPos,
			EType.IndustrialGooper,
			new Coord(-1, 0, 0),
			Coord.Up);
		gooper.State.ActionPoints = 0;

		var battle = BattleTestFixture.BeginSimulation(player, [ally, gooper]);
		var actions = await BattleTestFixture.AwaitUnitActions(battle, gooper);

		Assert.Contains(actions, action => action is GoopGunAction);
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
