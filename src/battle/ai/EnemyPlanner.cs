using GrimSpace.Battle.Board;
using GrimSpace.Core.Actions;
using GrimSpace.Core.Actions.Battle;
using GrimSpace.Math.Grid;
using GrimSpace.Battle.Units;
using BoundedGrid = GrimSpace.Math.Grid.Grid;

namespace GrimSpace.Battle.Ai;

public static class EnemyPlanner
{
	private const int MaxPlanLength = 16;
	private const int MomentumWeight = 1_000;
	private const int UnusedApPenalty = 100;

	public static HashSet<Coord> CollectHazardCells(
		IReadOnlySet<Coord> activeHazardCells,
		Unit player,
		IReadOnlyList<Unit> roster,
		BoundedGrid grid,
		IReadOnlyDictionary<string, NonUnit> nonUnits,
		IReadOnlySet<Coord> blockedCells,
		IEnumerable<IAction> playerActions)
	{
		var cells = new HashSet<Coord>(activeHazardCells);
		var sim = new TurnPlanner();
		sim.BeginTurn(player.State.Id, roster, grid, nonUnits, blockedCells);

		foreach (var action in playerActions)
			sim.ForceApplyAndEnqueue(BattleActionFactory.WithOwner(player.State.Id, action));

		foreach (var hazard in sim.Board.TurnHazards)
			cells.UnionWith(hazard.Cells);

		return cells;
	}

	public static TurnPlanner PlanTurn(
		Unit actor,
		IReadOnlyList<Unit> roster,
		BoundedGrid grid,
		IReadOnlyDictionary<string, NonUnit> nonUnits,
		IReadOnlySet<Coord> hazardCells,
		IReadOnlySet<Coord> blockedCells)
	{
		var plan = new TurnPlanner();
		plan.BeginTurn(actor.State.Id, roster, grid, nonUnits, blockedCells);

		var actorId = actor.State.Id;

		for (var step = 0; step < MaxPlanLength; step++)
		{
			var currentScore = ScoreTurn(plan, actorId, hazardCells);
			IAction? bestAction = null;
			var bestScore = currentScore;

			foreach (var candidate in LegalActions.EnumerateMovement(plan.Board, plan.Context, actorId))
			{
				if (!TryEnqueueTrial(plan, actorId, candidate))
					continue;

				if (plan.Board.StateOf(actorId).ActionPoints < 0)
				{
					plan.TryUndoLast();
					continue;
				}

				var score = ScoreTurn(plan, actorId, hazardCells);
				plan.TryUndoLast();

				if (score <= bestScore)
					continue;

				bestScore = score;
				bestAction = candidate;
			}

			if (bestAction is null || bestScore <= currentScore)
				break;

			TryEnqueueTrial(plan, actorId, bestAction);
		}

		return plan;
	}

	private static int ScoreTurn(TurnPlanner plan, string actorId, IReadOnlySet<Coord> hazardCells)
	{
		var state = plan.Board.StateOf(actorId);
		if (hazardCells.Contains(state.Position))
			return int.MinValue;

		var momentum = state.MomentumLevel;
		if (!plan.Actions.Any(action => action is MoveAction))
			momentum = System.Math.Max(0, momentum - 1);

		return momentum * MomentumWeight - state.ActionPoints * UnusedApPenalty;
	}

	private static bool TryEnqueueTrial(TurnPlanner plan, string ownerId, IAction candidate)
	{
		if (candidate is MoveAction && plan.Actions.Any(action => action is MoveAction))
			return false;

		return plan.TryApplyAndEnqueue(BattleActionFactory.WithOwner(ownerId, candidate));
	}
}
