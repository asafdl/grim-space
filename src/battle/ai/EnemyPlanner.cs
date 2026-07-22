using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Board;
using GrimSpace.Battle.Movement;
using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.Spatial;
using GrimSpace.Battle.Turn;
using GrimSpace.Battle.Units;
using GrimSpace.Core.Actions;
using GrimSpace.Core.Engine;
using GrimSpace.Math.Grid;
using BoundedGrid = GrimSpace.Math.Grid.Grid;
using BattleSimulation = GrimSpace.Core.Engine.Simulation<
	GrimSpace.Battle.Board.BattleBoard,
	GrimSpace.Battle.Runtime.ActorSession>;

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
		var plan = CreatePlan(roster, grid, nonUnits, blockedCells, turnStartTick, player.State.Id);

		foreach (var action in playerActions)
			plan.ForceEnqueue(action);

		plan.Refresh();
		BattleOrchestrator.ApplyEndOfPhase(plan.PreviewWorld, plan.PreviewRuntime, player.State.Id);

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
		var actorId = actor.State.Id;
		var unitType = actor.State.Type;
		var plan = CreatePlan(roster, grid, nonUnits, blockedCells, turnStartTick, actorId);

		for (var step = 0; step < MaxPlanLength; step++)
		{
			var currentScore = ScoreTurn(plan, actorId, hazardCells, turnStartTick);
			IAction? bestAction = null;
			Option? bestMove = null;
			var bestScore = currentScore;

			foreach (var candidate in Capabilities.For(unitType)
				.Where(def => def is not MoveDef)
				.SelectMany(def => def.Discover(plan.PreviewWorld, plan.PreviewRuntime, actorId)))
			{
				if (!TryEnqueueTrial(plan, candidate))
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

			foreach (var move in Capabilities.For(unitType)
				.OfType<MoveDef>()
				.SelectMany(def => def.DiscoverPaths(plan.PreviewWorld, plan.PreviewRuntime, actorId)))
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
				TryEnqueueTrial(plan, bestAction);
		}

		return plan.Actions;
	}

	private static BattleSimulation CreatePlan(
		IReadOnlyList<Unit> roster,
		BoundedGrid grid,
		IReadOnlyDictionary<string, NonUnit> nonUnits,
		IReadOnlySet<Coord> blockedCells,
		int turnStartTick,
		string ownerId)
	{
		var anchor = BattleBoard.FromSnapshot(roster, nonUnits, grid, blockedCells);
		var plan = new BattleSimulation(anchor, new ActorSession());
		plan.Begin(turnStartTick);
		BattleOrchestrator.ApplyEndOfPhase(plan.PreviewWorld, plan.PreviewRuntime, ownerId);
		return plan;
	}

	private static HashSet<Coord> CollectResolveHazardCells(BattleSimulation plan, int turnStartTick)
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
		BattleSimulation plan,
		string actorId,
		IReadOnlySet<Coord> hazardCells,
		int turnStartTick)
	{
		BattleOrchestrator.ApplyEndOfPhase(plan.PreviewWorld, plan.PreviewRuntime, actorId);

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

	private static bool TryEnqueueTrial(BattleSimulation plan, IAction candidate) =>
		candidate is IAction<BattleBoard, ActorSession> && plan.TryEnqueue(candidate);

	private static bool TryEnqueueMoveTrial(BattleSimulation plan, string ownerId, Option move) =>
		BattleOrchestrator.TryEnqueueMovePath(plan, ownerId, move);
}
