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
	int TurnStart,
	TurnPlanner PlayerPlan,
	TurnPlanner EnemyPlan);

public static class TurnCommit
{
	public static TurnCommitResult Build(
		FinalizedPlan playerPlan,
		Timeline timeline,
		IReadOnlyList<Unit> units,
		BoundedGrid grid,
		IReadOnlyDictionary<string, NonUnit> nonUnits,
		IReadOnlySet<Coord> hazardCells,
		IReadOnlySet<Coord> blockedCells)
	{
		var player = units.First(unit => unit.Controller == EController.Player);
		var enemy = units.First(unit => unit.Controller == EController.Enemy);
		var turnStart = timeline.Clock.Current;

		var playerTurnPlanner = new TurnPlanner();
		playerTurnPlanner.CopyFrom(playerPlan.Actions);

		var resolvedHazardCells = EnemyPlanner.CollectHazardCells(
			hazardCells,
			player,
			units,
			grid,
			nonUnits,
			blockedCells,
			playerPlan.Actions,
			turnStart);

		var enemyPlan = EnemyPlanner.PlanTurn(
			enemy,
			units,
			grid,
			nonUnits,
			resolvedHazardCells,
			blockedCells,
			turnStart);

		timeline.At(turnStart + TurnPhases.Player).EnqueueAll(playerPlan.Actions);
		timeline.At(turnStart + TurnPhases.Enemy).EnqueueAll(enemyPlan.Actions);

		return new TurnCommitResult(turnStart, playerTurnPlanner, enemyPlan);
	}
}
