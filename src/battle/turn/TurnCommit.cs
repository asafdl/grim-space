using GrimSpace.Battle.Ai;
using GrimSpace.Battle.Board;
using GrimSpace.Battle.Player;
using GrimSpace.Battle.Units;
using GrimSpace.Core.Actions;
using GrimSpace.Battle.Actions;
using GrimSpace.Core.Engine;
using GrimSpace.Math.Grid;
using GrimSpace.Units.Enums;
using BoundedGrid = GrimSpace.Math.Grid.Grid;

namespace GrimSpace.Battle.Turn;

public readonly record struct TurnCommitResult(
	int TurnStart,
	IReadOnlyList<IAction> EnemyActions);

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

		var resolvedHazardCells = EnemyPlanner.CollectHazardCells(
			hazardCells,
			player,
			units,
			grid,
			nonUnits,
			blockedCells,
			playerPlan.Actions,
			turnStart);

		var enemyActions = EnemyPlanner.PlanTurn(
			enemy,
			units,
			grid,
			nonUnits,
			resolvedHazardCells,
			blockedCells,
			turnStart);

		EnqueuePhase(timeline, turnStart + TurnPhases.Player, player.State.Id, playerPlan.Actions);
		EnqueuePhase(timeline, turnStart + TurnPhases.Enemy, enemy.State.Id, enemyActions);
		EnqueueRoundUpkeep(timeline, turnStart + TurnPhases.End, units);

		return new TurnCommitResult(turnStart, enemyActions);
	}

	private static void EnqueuePhase(
		Timeline timeline,
		int tick,
		string actorId,
		IReadOnlyList<IAction> actions)
	{
		timeline.At(tick).EnqueueAll(actions);
		timeline.At(tick).Enqueue(new EndOfPhaseAction(actorId));
	}

	private static void EnqueueRoundUpkeep(Timeline timeline, int tick, IReadOnlyList<Unit> units)
	{
		foreach (var unit in units)
			timeline.At(tick).Enqueue(new RoundUpkeepAction(unit.State.Id));

		timeline.At(tick).Enqueue(new ClearTurnHazardsAction());
	}
}
