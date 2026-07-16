using GrimSpace.Battle.Ai;
using GrimSpace.Battle.Board;
using GrimSpace.Battle.Player;
using GrimSpace.Battle.Units;
using GrimSpace.Core.Actions;
using GrimSpace.Core.Actions.Battle;
using GrimSpace.Math.Grid;
using GrimSpace.Units.Enums;
using BoundedGrid = GrimSpace.Math.Grid.Grid;

namespace GrimSpace.Battle.Turn;

public readonly record struct TurnCommitResult(
	GlobalActionQueue Queue,
	UnitPlan PlayerPlan,
	UnitPlan EnemyPlan);

public static class TurnCommit
{
	public static TurnCommitResult Build(
		FinalizedPlan playerPlan,
		IReadOnlyList<Unit> units,
		BoundedGrid grid,
		IReadOnlyDictionary<string, NonUnit> nonUnits,
		IReadOnlySet<Coord> hazardCells,
		IReadOnlySet<Coord> blockedCells)
	{
		var player = units.First(unit => unit.Controller == EController.Player);
		var enemy = units.First(unit => unit.Controller == EController.Enemy);

		var playerUnitPlan = new UnitPlan();
		playerUnitPlan.CopyFrom(player.State, playerPlan.Actions);

		var enemyPlan = EnemyPlanner.PlanTurn(
			enemy,
			units,
			grid,
			nonUnits,
			hazardCells,
			blockedCells);

		var queue = new GlobalActionQueue();
		queue.EnqueueAll(playerPlan.Actions);
		queue.EnqueueAll(enemyPlan.Actions);

		return new TurnCommitResult(queue, playerUnitPlan, enemyPlan);
	}
}
