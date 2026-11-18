using GrimSpace.Battle;
using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Ai;
using GrimSpace.Battle.Units;
using GrimSpace.Math.Grid;
using GrimSpace.Units;
using GrimSpace.Units.Enums;
using GrimSpace.Units.Loadouts.Abilities;

namespace GrimSpace.Tests.Ai;

[BattleTestSuite]
public sealed class VoidBombLaunchScoringTests
{
	[Fact]
	public async Task BuildTurnActions_LaunchesWhenOpponentLiesAlongTheNose()
	{
		var battle = Battle(new Coord(5, 2, 5));
		var actions = await BattleTestFixture.AwaitUnitActions(battle, EnemyOf(battle));

		Assert.Contains(actions, action => action is VoidBombAction);
	}

	[Fact]
	public async Task BuildTurnActions_HoldsWhenOpponentIsOutsideTheNose()
	{
		var battle = Battle(new Coord(5, 5, 10));
		var actions = await BattleTestFixture.AwaitUnitActions(battle, EnemyOf(battle));

		Assert.DoesNotContain(actions, action => action is VoidBombAction);
	}

	private static BattleOrchestrator Battle(Coord playerPosition)
	{
		var enemy = Factory.Create(
			ShipInstance.FromCatalog("enemy", EType.Fighter),
			ETeam.Enemy,
			new Coord(5, 5, 5),
			new AiController(),
			Coord.Forward,
			Coord.Up);
		enemy.State.CurrentActionPoints = 0;
		enemy.State.CurrentManeuverPoints = 0;
		foreach (var installed in enemy.State.Loadout.InstalledAbilities)
		{
			if (installed.Spec is IPerTurnAbility)
				enemy.State.MountRuntimeFor(installed.Mount).UsesRemaining = 0;
		}

		var player = Factory.Create(
			ShipInstance.FromCatalog("player", EType.Fighter),
			ETeam.Player,
			playerPosition,
			new AiController(),
			Coord.Forward,
			Coord.Up);
		return BattleTestFixture.BeginSimulation(player, enemy);
	}

	private static Unit EnemyOf(BattleOrchestrator battle) =>
		UnitRegistry.For(battle.Engine.World).UnitOf("enemy");
}
