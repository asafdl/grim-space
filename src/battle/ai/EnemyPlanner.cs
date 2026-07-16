using GrimSpace.Battle.Board;
using GrimSpace.Core.Actions.Battle;
using GrimSpace.Math.Grid;
using GrimSpace.Battle.Movement;
using GrimSpace.Battle.Movement.Enums;
using GrimSpace.Battle.Units;
using BoundedGrid = GrimSpace.Math.Grid.Grid;

namespace GrimSpace.Battle.Ai;

public static class EnemyPlanner
{
	private const int MaxPlanLength = 16;
	private const int HazardDeathPenalty = 1_000_000;
	private const int EscapeHazardBonus = 500_000;
	private const int MomentumReductionBonus = 1_000;
	private const int MovementBonus = 50;

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

		var startedInHazard = hazardCells.Contains(actor.State.Position);
		var startMomentum = actor.State.MomentumLevel;
		var actorId = actor.State.Id;

		for (var step = 0; step < MaxPlanLength; step++)
		{
			var currentState = plan.Board.StateOf(actorId);
			var currentScore = ScoreState(currentState, startMomentum, hazardCells, startedInHazard);

			IBattleAction? bestAction = null;
			var bestScore = currentScore;

			foreach (var candidate in LegalActions.EnumerateMovement(plan.Board, plan.Context, actorId, blockedCells))
			{
				if (!TryEnqueueTrial(plan, actorId, candidate))
					continue;

				var actorState = plan.Board.StateOf(actorId);
				plan.TryUndoLast();

				if (actorState.ActionPoints < 0)
					continue;

				var score = ScoreState(actorState, startMomentum, hazardCells, startedInHazard);
				if (candidate is MoveAction move)
					score += MovementBonus - move.Option.ApCost;

				if (score <= bestScore)
					continue;

				bestScore = score;
				bestAction = candidate;
			}

			if (bestAction is null)
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

	private static int ScoreState(
		State state,
		int startMomentum,
		IReadOnlySet<Coord> hazardCells,
		bool startedInHazard)
	{
		var score = ScorePosition(state.Position, hazardCells, startedInHazard);
		score += (startMomentum - state.MomentumLevel) * MomentumReductionBonus;
		return score;
	}

	private static int ScorePosition(Coord position, IReadOnlySet<Coord> hazardCells, bool startedInHazard)
	{
		if (hazardCells.Contains(position))
			return -HazardDeathPenalty;

		if (startedInHazard)
			return EscapeHazardBonus;

		return 0;
	}
}
