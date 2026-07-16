using GrimSpace.Battle.Ai;
using GrimSpace.Battle.Board;
using GrimSpace.Battle.Units;
using GrimSpace.Battle.Weapons;
using GrimSpace.Core.Actions;
using GrimSpace.Core.Actions.Battle;
using GrimSpace.Math.Grid;
using GrimSpace.Tests.Movement;

namespace GrimSpace.Tests.Ai;

public sealed class EnemyPlannerTests
{
	[Fact]
	public void PlanEndsOutsideHazardWhenEscapeExists()
	{
		var origin = new Coord(5, 5, 5);
		var enemy = BattleTestFixture.Enemy(origin);
		var player = BattleTestFixture.Player(new Coord(0, 0, 0));
		var grid = BattleTestFixture.Grid();
		var blocked = new HashSet<Coord> { player.State.Position };
		var hazardCells = new HashSet<Coord> { origin };

		var plan = EnemyPlanner.PlanTurn(
			enemy,
			[enemy, player],
			grid,
			new Dictionary<string, NonUnit>(),
			hazardCells,
			blocked);

		var end = plan.Board.StateOf(enemy.State.Id).Position;
		Assert.DoesNotContain(end, hazardCells);
	}

	[Fact]
	public void PlanPrefersMoveThatBuildsMomentumWhenSafe()
	{
		var origin = new Coord(5, 5, 5);
		var enemy = BattleTestFixture.Enemy(origin, momentum: 0);
		var player = BattleTestFixture.Player(new Coord(0, 0, 0));
		var grid = BattleTestFixture.Grid();
		var blocked = new HashSet<Coord> { player.State.Position };
		var hazardCells = new HashSet<Coord>();

		var plan = EnemyPlanner.PlanTurn(
			enemy,
			[enemy, player],
			grid,
			new Dictionary<string, NonUnit>(),
			hazardCells,
			blocked);

		Assert.Contains(plan.BattleActions, action => action is MoveAction);
		Assert.True(plan.Board.StateOf(enemy.State.Id).MomentumLevel > 0);
	}

	[Fact]
	public void PlanTakesMoveWhenMomentumWouldDecayOtherwise()
	{
		var origin = new Coord(5, 5, 5);
		var enemy = BattleTestFixture.Enemy(origin, momentum: 2);
		var player = BattleTestFixture.Player(new Coord(0, 0, 0));
		var grid = BattleTestFixture.Grid();
		var blocked = new HashSet<Coord> { player.State.Position };

		var plan = EnemyPlanner.PlanTurn(
			enemy,
			[enemy, player],
			grid,
			new Dictionary<string, NonUnit>(),
			new HashSet<Coord>(),
			blocked);

		Assert.NotEmpty(plan.BattleActions);
		Assert.True(plan.Board.StateOf(enemy.State.Id).MomentumLevel >= 2);
	}

	[Fact]
	public void CollectHazardCellsIncludesPlayerPlannedMissiles()
	{
		var origin = new Coord(5, 5, 1);
		var target = origin + Coord.Forward * CombatConfig.DorsalMissileMinRange;
		var player = BattleTestFixture.Player(origin);
		var enemy = BattleTestFixture.Enemy(new Coord(0, 0, 0));
		var grid = BattleTestFixture.Grid();
		var blocked = new HashSet<Coord> { enemy.State.Position };
		var playerActions = new IAction[]
		{
			new MissileAction(
				player.State.Id,
				target,
				EMissileMount.Dorsal,
				CombatConfig.DorsalMissileMinRange),
		};

		var cells = EnemyPlanner.CollectHazardCells(
			new HashSet<Coord>(),
			player,
			[player, enemy],
			grid,
			new Dictionary<string, NonUnit>(),
			blocked,
			playerActions);

		Assert.Contains(target, cells);
	}
}
