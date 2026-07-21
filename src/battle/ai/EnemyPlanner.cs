using GrimSpace.Battle.Board;
using GrimSpace.Battle.Turn;
using GrimSpace.Battle.Movement;
using GrimSpace.Core.Actions;
using GrimSpace.Core.Actions.Battle;
using GrimSpace.Battle.Actions;
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
		IEnumerable<IAction> playerActions,
		int turnStartTick)
	{
		var cells = new HashSet<Coord>(activeHazardCells);
		var sim = new BattleSession();
		sim.BeginTurn(player.State.Id, roster, grid, nonUnits, blockedCells, turnStartTick);

		foreach (var action in playerActions)
			sim.ForceApplyAndEnqueue(BattleActionFactory.WithOwner(player.State.Id, action));

		foreach (var hazard in sim.Board.TurnHazards)
			cells.UnionWith(hazard.Cells);

		cells.UnionWith(CollectResolveHazardCells(sim, turnStartTick));

		return cells;
	}

	public static BattleSession PlanTurn(
		Unit actor,
		IReadOnlyList<Unit> roster,
		BoundedGrid grid,
		IReadOnlyDictionary<string, NonUnit> nonUnits,
		IReadOnlySet<Coord> hazardCells,
		IReadOnlySet<Coord> blockedCells,
		int turnStartTick)
	{
		var plan = new BattleSession();
		plan.BeginTurn(actor.State.Id, roster, grid, nonUnits, blockedCells, turnStartTick);

		var actorId = actor.State.Id;

		for (var step = 0; step < MaxPlanLength; step++)
		{
			var currentScore = ScoreTurn(plan, actorId, hazardCells, turnStartTick);
			IAction? bestAction = null;
			Option? bestMove = null;
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

				var score = ScoreTurn(plan, actorId, hazardCells, turnStartTick);
				plan.TryUndoLast();

				if (score <= bestScore)
					continue;

				bestScore = score;
				bestAction = candidate;
				bestMove = null;
			}

			foreach (var move in LegalActions.EnumerateMovePaths(plan.Board, plan.Context, actorId))
			{
				if (!TryEnqueueMoveTrial(plan, actorId, move))
					continue;

				if (plan.Board.StateOf(actorId).ActionPoints < 0)
				{
					plan.TryUndoLast();
					continue;
				}

				var score = ScoreTurn(plan, actorId, hazardCells, turnStartTick);
				plan.TryUndoLast();

				if (score <= bestScore)
					continue;

				bestScore = score;
				bestAction = null;
				bestMove = move;
			}

			if (bestAction is null && bestMove is null)
				break;

			if (bestScore <= currentScore)
				break;

			if (bestMove is not null)
				TryEnqueueMoveTrial(plan, actorId, bestMove);
			else if (bestAction is not null)
				TryEnqueueTrial(plan, actorId, bestAction);
		}

		return plan;
	}

	private static HashSet<Coord> CollectResolveHazardCells(BattleSession sim, int turnStartTick)
	{
		var cells = new HashSet<Coord>();
		foreach (var (tick, action) in sim.PreviewTimeline.From(turnStartTick + TurnPhases.Enemy))
		{
			if (action is not ResolveHazardAction resolve)
				continue;

			if (!sim.Board.NonUnits.TryGetValue(resolve.HazardId, out var nonUnit) || nonUnit is not Hazard hazard)
				continue;

			if (tick <= turnStartTick + TurnPhases.Enemy)
				cells.UnionWith(hazard.Cells);
		}

		return cells;
	}

	private static int ScoreTurn(
		BattleSession plan,
		string actorId,
		IReadOnlySet<Coord> hazardCells,
		int turnStartTick)
	{
		var state = plan.Board.StateOf(actorId);
		if (hazardCells.Contains(state.Position))
			return int.MinValue;

		foreach (var (tick, action) in plan.PreviewTimeline.From(turnStartTick + TurnPhases.Enemy))
		{
			if (action is not ResolveHazardAction resolve)
				continue;

			if (!plan.Board.NonUnits.TryGetValue(resolve.HazardId, out var nonUnit) || nonUnit is not Hazard hazard)
				continue;

			if (tick <= turnStartTick + TurnPhases.Enemy && hazard.Cells.Contains(state.Position))
				return int.MinValue / 2;
		}

		return state.MomentumLevel * MomentumWeight - state.ActionPoints * UnusedApPenalty;
	}

	private static bool TryEnqueueTrial(BattleSession plan, string ownerId, IAction candidate) =>
		plan.TryApplyAndEnqueue(BattleActionFactory.WithOwner(ownerId, candidate));

	private static bool TryEnqueueMoveTrial(BattleSession plan, string ownerId, Option move) =>
		plan.TryEnqueueMovePath(ownerId, move);
}
