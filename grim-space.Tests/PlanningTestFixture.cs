using GrimSpace.Battle.Player;
using GrimSpace.Battle.Units;
using GrimSpace.Math.Grid;
using BoundedGrid = GrimSpace.Math.Grid.Grid;

namespace GrimSpace.Tests;

internal static class PlanningTestFixture
{
	public static PlayerController Controller(
		Unit player,
		Unit enemy,
		BoundedGrid? grid = null,
		IReadOnlySet<Coord>? blocked = null)
	{
		grid ??= BattleTestFixture.Grid();
		blocked ??= new HashSet<Coord> { enemy.State.Position };
		return new PlayerController(player, enemy, grid, blocked, _ => true, () => player);
	}
}
