using GrimSpace.Battle.Board;
using GrimSpace.Core.Actions;
using GrimSpace.Core.Actions.Battle;
using GrimSpace.Math.Grid;
using GrimSpace.Battle.Movement;
using GrimSpace.Battle.Units;
using BoundedGrid = GrimSpace.Math.Grid.Grid;

namespace GrimSpace.Battle.Ai;

public static class EnemyPlanner
{
	private const int MaxPlanLength = 16;

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
		var sim = new UnitPlan();
		sim.BeginTurn(player.State.Id, roster, grid, nonUnits, blockedCells);

		foreach (var action in playerActions)
		{
			if (action is IBattleAction battleAction)
				sim.ForceApplyAndEnqueue(BattleActionFactory.AsQueued(player.State.Id, battleAction));
		}

		foreach (var hazard in sim.Board.TurnHazards)
			cells.UnionWith(hazard.Cells);

		return cells;
	}

	public static UnitPlan PlanTurn(
		Unit actor,
		IReadOnlyList<Unit> roster,
		BoundedGrid grid,
		IReadOnlyDictionary<string, NonUnit> nonUnits,
		IReadOnlySet<Coord> hazardCells,
		IReadOnlySet<Coord> blockedCells)
	{
		var plan = new UnitPlan();
		plan.BeginTurn(actor.State.Id, roster, grid, nonUnits, blockedCells);

		var actorId = actor.State.Id;

		for (var step = 0; step < MaxPlanLength; step++)
		{
			var currentScore = MomentumAtTurnEnd(plan, actorId, hazardCells);
			IBattleAction? bestAction = null;
			var bestScore = currentScore;

			foreach (var candidate in LegalActions.EnumerateMovement(plan.Board, plan.Context, actorId, blockedCells))
			{
				if (!TryEnqueueTrial(plan, actorId, candidate))
					continue;

				if (plan.Board.StateOf(actorId).ActionPoints < 0)
				{
					plan.TryUndoLast();
					continue;
				}

				var score = ScoreBestTurnEnd(plan, actorId, hazardCells);
				plan.TryUndoLast();

				if (score < bestScore)
					continue;

				if (score == bestScore && bestAction is not null)
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

	private static bool TryEnqueueTrial(UnitPlan plan, string ownerId, IBattleAction candidate)
	{
		if (candidate is MoveAction && plan.Actions.Any(action => action is MoveAction))
			return false;

		return plan.TryApplyAndEnqueue(BattleActionFactory.AsQueued(ownerId, candidate));
	}

	/// <summary>
	/// Best achievable end-of-turn momentum: safe position required; includes one optional move lookahead.
	/// </summary>
	private static int ScoreBestTurnEnd(UnitPlan plan, string actorId, IReadOnlySet<Coord> hazardCells)
	{
		var stopNow = MomentumAtTurnEnd(plan, actorId, hazardCells);
		if (stopNow == int.MinValue)
			return int.MinValue;

		if (plan.Actions.Any(action => action is MoveAction))
			return stopNow;

		var best = stopNow;
		foreach (var option in LegalActions.GetMoveOptions(plan.Board, plan.Context, actorId))
		{
			var move = new MoveAction(actorId, option);
			if (!TryEnqueueTrial(plan, actorId, move))
				continue;

			best = System.Math.Max(best, MomentumAtTurnEnd(plan, actorId, hazardCells));
			plan.TryUndoLast();
		}

		return best;
	}

	private static int MomentumAtTurnEnd(UnitPlan plan, string actorId, IReadOnlySet<Coord> hazardCells)
	{
		var state = plan.Board.StateOf(actorId);
		if (hazardCells.Contains(state.Position))
			return int.MinValue;

		var momentum = state.MomentumLevel;
		if (!plan.BattleActions.Any(action => action is MoveAction))
			momentum = System.Math.Max(0, momentum - 1);

		return momentum;
	}
}
