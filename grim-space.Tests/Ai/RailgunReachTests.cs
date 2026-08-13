using GrimSpace.Battle;
using GrimSpace.Battle.Ai;
using GrimSpace.Battle.Movement;
using GrimSpace.Battle.Units;
using GrimSpace.Battle.Abilities;
using GrimSpace.Math.Grid;
using GrimSpace.Units;
using GrimSpace.Units.Enums;

namespace GrimSpace.Tests.Ai;

public sealed class RailgunReachTests
{
	[Fact]
	public void CouldPossiblyDamage_WhenWithinWeaponReachAlone_ReturnsTrue()
	{
		var self = Coord.Zero;
		var opponent = new Coord(CombatConfig.MaxRailgunManhattanRange, 0, 0);

		Assert.True(OffensiveReach.CouldPossiblyDamage(self, actionPoints: 0, opponent, CombatConfig.MaxRailgunManhattanRange));
	}

	[Fact]
	public void CouldPossiblyDamage_WhenJustBeyondReachAndMoveBubble_ReturnsFalse()
	{
		var ap = 2;
		var reach = CombatConfig.MaxRailgunManhattanRange;
		var bubble = OffensiveReach.OptimisticMoveBubble(ap);
		var self = Coord.Zero;
		var opponent = new Coord(bubble + reach + 1, 0, 0);

		Assert.False(OffensiveReach.CouldPossiblyDamage(self, ap, opponent, reach));
	}

	[Fact]
	public void CouldPossiblyDamage_WhenMoveBubbleClosesTheGap_ReturnsTrue()
	{
		var ap = 5;
		var reach = CombatConfig.MaxRailgunManhattanRange;
		var bubble = OffensiveReach.OptimisticMoveBubble(ap);
		var self = Coord.Zero;
		var opponent = new Coord(bubble + reach, 0, 0);

		Assert.True(OffensiveReach.CouldPossiblyDamage(self, ap, opponent, reach));
	}

	[Fact]
	public void OptimisticMoveBubble_IncludesMaxFreeForwardSteps()
	{
		var free = MomentumConfig.ForLevel(MomentumConfig.MaxLevel).FreeForwardSteps;
		Assert.Equal(4 + free, OffensiveReach.OptimisticMoveBubble(4));
	}

	[Fact]
	public void UpperBound_OmitsDamageBonus_WhenOpponentOutOfOptimisticReach()
	{
		var ap = 0;
		var gap = OffensiveReach.OptimisticMoveBubble(ap) + CombatConfig.MaxRailgunManhattanRange + 1;
		var player = CreateUnit(Alliance.Player, "player", new Coord(gap, 5, 5), EType.Fighter);
		var enemy = CreateUnit(Alliance.Enemy, "enemy", new Coord(0, 5, 5), EType.Carrier);
		enemy.State.ActionPoints = ap;

		var battle = BattleTestFixture.BeginSimulation(player, enemy, BattleTestFixture.Grid(size: 32));
		var bound = EnemySearchInput.UpperBound(battle.Engine.World, enemy.State.Id);

		Assert.Equal(MomentumConfig.MaxLevel * EnemySearchInput.MomentumWeight, bound);
	}

	[Fact]
	public void UpperBound_IncludesDamageBonus_WhenOpponentInOptimisticReach()
	{
		var player = CreateUnit(Alliance.Player, "player", new Coord(6, 5, 5), EType.Fighter);
		var enemy = CreateUnit(Alliance.Enemy, "enemy", new Coord(0, 5, 5), EType.Carrier);
		enemy.State.ActionPoints = 0;

		var battle = BattleTestFixture.BeginSimulation(player, enemy);
		var bound = EnemySearchInput.UpperBound(battle.Engine.World, enemy.State.Id);

		Assert.Equal(
			MomentumConfig.MaxLevel * EnemySearchInput.MomentumWeight + EnemySearchInput.DamageHitBonus,
			bound);
	}

	[Fact]
	public void UpperBound_IncludesDamageBonus_WhenPatrolCanReachPlayerWithFlak()
	{
		var player = CreateUnit(Alliance.Player, "player", new Coord(4, 5, 5), EType.Fighter);
		var patrol = CreateUnit(Alliance.Enemy, "patrol", new Coord(0, 5, 5), EType.Patrol);
		patrol.State.ActionPoints = 0;

		var battle = BattleTestFixture.BeginSimulation(player, patrol);
		var bound = EnemySearchInput.UpperBound(battle.Engine.World, patrol.State.Id);

		Assert.Equal(
			MomentumConfig.MaxLevel * EnemySearchInput.MomentumWeight + EnemySearchInput.DamageHitBonus,
			bound);
	}

	private static Unit CreateUnit(Alliance alliance, string id, Coord position, EType type) =>
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
			new Coord(1, 0, 0),
			Coord.Up);
}
