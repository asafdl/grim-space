using GrimSpace.Battle.Board;
using GrimSpace.Battle.Movement;
using GrimSpace.Battle.Player;
using GrimSpace.Battle.Units;
using GrimSpace.Battle.Actions;
using GrimSpace.Math.Grid;
using BoundedGrid = GrimSpace.Math.Grid.Grid;

namespace GrimSpace.Tests;

internal static class PlanningTestFixture
{
	public static PlayerController Controller(
		Unit player,
		Unit enemy,
		BoundedGrid? grid = null,
		IReadOnlySet<Coord>? blocked = null,
		IReadOnlyDictionary<string, NonUnit>? nonUnits = null)
	{
		grid ??= BattleTestFixture.Grid();
		blocked ??= new HashSet<Coord> { enemy.State.Position };
		nonUnits ??= new Dictionary<string, NonUnit>();
		var roster = new[] { player, enemy };
		return new PlayerController(player, enemy, roster, nonUnits, grid, blocked, _ => true, () => player);
	}
}
