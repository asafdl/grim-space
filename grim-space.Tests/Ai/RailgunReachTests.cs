using GrimSpace.Battle;
using GrimSpace.Battle.Ai;
using GrimSpace.Battle.Movement;
using GrimSpace.Battle.Units;
using GrimSpace.Battle.Weapons;
using GrimSpace.Math.Grid;
using GrimSpace.Units;
using GrimSpace.Units.Enums;

namespace GrimSpace.Tests.Ai;

public sealed class RailgunReachTests
{
	[Fact]
	public void CouldPossiblyHit_WhenWithinRailRangeAlone_ReturnsTrue()
	{
		var self = Coord.Zero;
		var opponent = new Coord(CombatConfig.MaxRailgunManhattanRange, 0, 0);

		Assert.True(RailgunReach.CouldPossiblyHit(self, actionPoints: 0, opponent));
	}

	[Fact]
	public void CouldPossiblyHit_WhenJustBeyondRailAndMoveBubble_ReturnsFalse()
	{
		var ap = 2;
		var bubble = RailgunReach.OptimisticMoveBubble(ap);
		var self = Coord.Zero;
		var opponent = new Coord(bubble + CombatConfig.MaxRailgunManhattanRange + 1, 0, 0);

		Assert.False(RailgunReach.CouldPossiblyHit(self, ap, opponent));
	}

	[Fact]
	public void CouldPossiblyHit_WhenMoveBubbleClosesTheGap_ReturnsTrue()
	{
		var ap = 5;
		var bubble = RailgunReach.OptimisticMoveBubble(ap);
		var self = Coord.Zero;
		var opponent = new Coord(bubble + CombatConfig.MaxRailgunManhattanRange, 0, 0);

		Assert.True(RailgunReach.CouldPossiblyHit(self, ap, opponent));
	}

	[Fact]
	public void OptimisticMoveBubble_IncludesMaxFreeForwardSteps()
	{
		var free = MomentumConfig.ForLevel(MomentumConfig.MaxLevel).FreeForwardSteps;
		Assert.Equal(4 + free, RailgunReach.OptimisticMoveBubble(4));
	}

	[Fact]
	public void UpperBound_OmitsRailgunBonus_WhenOpponentOutOfOptimisticReach()
	{
		var ap = 0;
		var gap = RailgunReach.OptimisticMoveBubble(ap) + CombatConfig.MaxRailgunManhattanRange + 1;
		var player = CreateUnit(Alliance.Player, "player", new Coord(gap, 5, 5), EType.Fighter);
		var enemy = CreateUnit(Alliance.Enemy, "enemy", new Coord(0, 5, 5), EType.Patrol);
		enemy.State.ActionPoints = ap;

		var battle = BattleTestFixture.BeginSimulation(player, enemy, BattleTestFixture.Grid(size: 32));
		var bound = EnemySearchInput.UpperBound(battle.Engine.World, enemy.State.Id);

		Assert.Equal(MomentumConfig.MaxLevel * EnemySearchInput.MomentumWeight, bound);
	}

	[Fact]
	public void UpperBound_IncludesRailgunBonus_WhenOpponentInOptimisticReach()
	{
		var player = CreateUnit(Alliance.Player, "player", new Coord(6, 5, 5), EType.Fighter);
		var enemy = CreateUnit(Alliance.Enemy, "enemy", new Coord(0, 5, 5), EType.Patrol);
		enemy.State.ActionPoints = 0;

		var battle = BattleTestFixture.BeginSimulation(player, enemy);
		var bound = EnemySearchInput.UpperBound(battle.Engine.World, enemy.State.Id);

		Assert.Equal(
			MomentumConfig.MaxLevel * EnemySearchInput.MomentumWeight + EnemySearchInput.RailgunHitBonus,
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
			null,
			0,
			new Coord(1, 0, 0),
			Coord.Up);
}
