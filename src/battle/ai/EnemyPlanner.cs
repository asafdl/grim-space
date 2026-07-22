using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Board;
using GrimSpace.Battle.Planning;
using GrimSpace.Battle.Turn;
using GrimSpace.Battle.Movement;
using GrimSpace.Core.Actions;
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
		var plan = CreatePlan();
		Begin(plan, player.State.Id, roster, grid, nonUnits, blockedCells, turnStartTick);

		foreach (var action in playerActions)
		{
			var owned = BattleActionFactory.WithOwner(player.State.Id, action);
			if (BattleActionRunner.IsKnown(owned))
				plan.ForceEnqueue(owned);
		}

		foreach (var hazard in plan.PreviewWorld.TurnHazards)
			cells.UnionWith(hazard.Cells);

		cells.UnionWith(CollectResolveHazardCells(plan, turnStartTick));

		return cells;
	}

	public static IReadOnlyList<IAction> PlanTurn(
		Unit actor,
		IReadOnlyList<Unit> roster,
		BoundedGrid grid,
		IReadOnlyDictionary<string, NonUnit> nonUnits,
		IReadOnlySet<Coord> hazardCells,
		IReadOnlySet<Coord> blockedCells,
		int turnStartTick)
	{
		var plan = CreatePlan();
		Begin(plan, actor.State.Id, roster, grid, nonUnits, blockedCells, turnStartTick);

		var actorId = actor.State.Id;

		for (var step = 0; step < MaxPlanLength; step++)
		{
			var currentScore = ScoreTurn(plan, actorId, hazardCells, turnStartTick);
			IAction? bestAction = null;
			Option? bestMove = null;
			var bestScore = currentScore;
			var ctx = BattleActionContext.For(plan.PreviewWorld, plan.PreviewRuntime, actorId);

			foreach (var candidate in ActionQueries.EnumerateMovement(ctx, actorId))
			{
				if (!TryEnqueueTrial(plan, actorId, candidate))
					continue;

				if (plan.PreviewWorld.StateOf(actorId).ActionPoints < 0)
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

			foreach (var move in ActionQueries.EnumerateMovePaths(plan.PreviewWorld, ctx, actorId))
			{
				if (!TryEnqueueMoveTrial(plan, actorId, move))
					continue;

				if (plan.PreviewWorld.StateOf(actorId).ActionPoints < 0)
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

		return plan.Actions;
	}

	private static PlanSimulation CreatePlan() => new();

	private static void Begin(
		PlanSimulation plan,
		string ownerId,
		IReadOnlyList<Unit> roster,
		BoundedGrid grid,
		IReadOnlyDictionary<string, NonUnit> nonUnits,
		IReadOnlySet<Coord> blockedCells,
		int turnStartTick)
	{
		var anchor = BattleBoard.FromSnapshot(roster, nonUnits, grid, blockedCells);
		plan.Begin(anchor, new TurnPhaseContext(), turnStartTick, ownerId);
	}

	private static HashSet<Coord> CollectResolveHazardCells(PlanSimulation plan, int turnStartTick)
	{
		var cells = new HashSet<Coord>();
		foreach (var (tick, action) in plan.PreviewWorld.Timeline.From(turnStartTick + TurnPhases.Enemy))
		{
			if (action is not ResolveHazardAction resolve)
				continue;

			if (!plan.PreviewWorld.NonUnits.TryGetValue(resolve.HazardId, out var nonUnit) || nonUnit is not Hazard hazard)
				continue;

			if (tick <= turnStartTick + TurnPhases.Enemy)
				cells.UnionWith(hazard.Cells);
		}

		return cells;
	}

	private static int ScoreTurn(
		PlanSimulation plan,
		string actorId,
		IReadOnlySet<Coord> hazardCells,
		int turnStartTick)
	{
		var state = plan.PreviewWorld.StateOf(actorId);
		if (hazardCells.Contains(state.Position))
			return int.MinValue;

		foreach (var (tick, action) in plan.PreviewWorld.Timeline.From(turnStartTick + TurnPhases.Enemy))
		{
			if (action is not ResolveHazardAction resolve)
				continue;

			if (!plan.PreviewWorld.NonUnits.TryGetValue(resolve.HazardId, out var nonUnit) || nonUnit is not Hazard hazard)
				continue;

			if (tick <= turnStartTick + TurnPhases.Enemy && hazard.Cells.Contains(state.Position))
				return int.MinValue / 2;
		}

		return state.MomentumLevel * MomentumWeight - state.ActionPoints * UnusedApPenalty;
	}

	private static bool TryEnqueueTrial(PlanSimulation plan, string ownerId, IAction candidate)
	{
		var owned = BattleActionFactory.WithOwner(ownerId, candidate);
		return BattleActionRunner.IsKnown(owned) && plan.TryEnqueue(owned);
	}

	private static bool TryEnqueueMoveTrial(PlanSimulation plan, string ownerId, Option move)
	{
		var actor = plan.PreviewWorld.StateOf(ownerId);
		var frame = GrimSpace.Battle.Spatial.BodyFrame.From(actor);
		foreach (var step in MoveDef.StepsFromPath(ownerId, frame, actor.Position, move.Path))
		{
			if (!plan.TryEnqueue(step))
				return false;
		}

		return true;
	}
}
