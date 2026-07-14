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

	public static IReadOnlyList<IBattleAction> PlanTurn(
		Unit actor,
		Unit opponent,
		BoundedGrid grid,
		IReadOnlySet<Coord> hazardCells)
	{
		var startFacing = GridBasis.From(
			actor.State.ForwardDirection,
			actor.State.UpDirection,
			actor.State.RightDirection);
		var startedInHazard = hazardCells.Contains(actor.State.Position);
		var plan = new List<IBattleAction>();

		for (var step = 0; step < MaxPlanLength; step++)
		{
			var currentScore = ScorePlan(actor, opponent, grid, plan, startFacing, hazardCells, startedInHazard);

			IBattleAction? bestAction = null;
			var bestScore = currentScore;

			foreach (var candidate in LegalActions.EnumerateMovement(actor, opponent, grid, plan, startFacing))
			{
				var trial = AppendAction(plan, candidate);
				var actorState = PlanSimulator.Simulate(actor, opponent, grid, trial, startFacing).Player;
				if (actorState.ActionPoints < 0)
					continue;

				var score = ScorePosition(actorState.Position, hazardCells, startedInHazard);
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

	private static int ScorePlan(
		Unit actor,
		Unit opponent,
		BoundedGrid grid,
		IReadOnlyList<IBattleAction> plan,
		GridBasis startFacing,
		IReadOnlySet<Coord> hazardCells,
		bool startedInHazard)
	{
		var position = PlanSimulator.Simulate(actor, opponent, grid, plan, startFacing).Player.Position;
		return ScorePosition(position, hazardCells, startedInHazard);
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
