using GrimSpace.Battle.Board;
using GrimSpace.Battle.Movement;
using GrimSpace.Battle.Units;
using GrimSpace.Math.Grid;
using BoundedGrid = GrimSpace.Math.Grid.Grid;

namespace GrimSpace.Tests;

internal static class PlanningTestFixture
{
	public static TestPlan Controller(
		Unit player,
		Unit enemy,
		BoundedGrid? grid = null,
		IReadOnlySet<Coord>? blocked = null,
		IReadOnlyDictionary<string, NonUnit>? nonUnits = null)
	{
		grid ??= BattleTestFixture.Grid();
		blocked ??= new HashSet<Coord> { enemy.State.Position };
		_ = nonUnits;
		return TestPlan.Begin(player.State.Id, player, enemy, grid, blocked);
	}
}
