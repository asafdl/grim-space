using GrimSpace.Core.Actions.Battle;
using GrimSpace.Math.Grid;
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

	public static IReadOnlyList<IBattleAction> PlanTurn(
		Unit actor,
		Unit opponent,
		BoundedGrid grid,
		IReadOnlySet<Coord> hazardCells,
		IReadOnlySet<Coord> blockedCells)
	{
		var startFacing = GridBasis.From(
			actor.State.ForwardDirection,
			actor.State.UpDirection,
			actor.State.RightDirection);
		var startedInHazard = hazardCells.Contains(actor.State.Position);
		var startMomentum = actor.State.MomentumLevel;
		var plan = new List<IBattleAction>();

		for (var step = 0; step < MaxPlanLength; step++)
		{
			var currentState = PlanSimulator.Simulate(actor, opponent, grid, plan, startFacing, blockedCells).Player;
			var currentScore = ScoreState(currentState, startMomentum, hazardCells, startedInHazard);

			IBattleAction? bestAction = null;
			var bestScore = currentScore;

			foreach (var candidate in LegalActions.EnumerateMovement(actor, opponent, grid, plan, startFacing, blockedCells))
			{
				var trial = AppendAction(plan, candidate);
				var actorState = PlanSimulator.Simulate(actor, opponent, grid, trial, startFacing, blockedCells).Player;
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

			AppendActionInPlace(plan, bestAction);
		}

		return plan;
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

	private static List<IBattleAction> AppendAction(IReadOnlyList<IBattleAction> plan, IBattleAction action)
	{
		var trial = plan.ToList();
		AppendActionInPlace(trial, action);
		return trial;
	}

	private static void AppendActionInPlace(List<IBattleAction> plan, IBattleAction action)
	{
		if (action is MoveAction)
		{
			var index = plan.FindIndex(queued => queued is MoveAction);
			if (index >= 0)
				plan[index] = action;
			else
				plan.Add(action);
			return;
		}

		plan.Add(action);
	}
}
