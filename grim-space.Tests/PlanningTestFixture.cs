using GrimSpace.Battle;
using GrimSpace.Battle.Units;
using GrimSpace.Math.Grid;
using BoundedGrid = GrimSpace.Math.Grid.Grid;

namespace GrimSpace.Tests;

internal static class PlanningTestFixture
{
	public static BattleOrchestrator Controller(
		Unit player,
		Unit enemy,
		BoundedGrid? grid = null,
		IReadOnlySet<Coord>? blocked = null) =>
		BattleTestFixture.BeginPlanning(player, enemy, grid, blocked);
}
