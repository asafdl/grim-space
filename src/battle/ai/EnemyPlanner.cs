using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Board;
using GrimSpace.Battle.Movement;
using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.Turn;
using GrimSpace.Battle.Units;
using GrimSpace.Core.Actions;
using GrimSpace.Core.Engine;
using BattleSimulation = GrimSpace.Core.Engine.Simulation<
	GrimSpace.Battle.Board.BattleBoard,
	GrimSpace.Battle.Runtime.ActorSession>;

namespace GrimSpace.Battle.Ai;

public static class EnemyPlanner
{
	private const int MaxPlanLength = 16;
	private const int MomentumWeight = 1_000;
	private const int UnusedApPenalty = 100;

	public static IReadOnlyList<IAction> PlanTurn(BattleSimulation session, Unit actor)
	{
		var actorId = actor.State.Id;
		var unitType = actor.State.Type;
		var runtime = session.PreviewActorRuntimes.For(actorId);
		var enemyActionStart = session.Actions.Count;

		for (var step = 0; step < MaxPlanLength; step++)
		{
			var currentScore = ScoreTurn(session, actorId);
			IAction? bestAction = null;
			Option? bestMove = null;
			var bestScore = currentScore;

			foreach (var candidate in Capabilities.For(unitType)
				.Where(def => def is not MoveDef)
				.SelectMany(def => def.Discover(session.PreviewWorld, runtime, actorId)))
			{
				if (!TryEnqueueTrial(session, candidate))
					continue;

				if (session.PreviewWorld.StateOf(actorId).ActionPoints < 0)
				{
					session.TryUndoLast();
					continue;
				}

				var score = ScoreTurn(session, actorId);
				session.TryUndoLast();

				if (score <= bestScore)
					continue;

				bestScore = score;
				bestAction = candidate;
				bestMove = null;
			}

			foreach (var move in Capabilities.For(unitType)
				.OfType<MoveDef>()
				.SelectMany(def => def.DiscoverPaths(session.PreviewWorld, runtime, actorId)))
			{
				if (!TryEnqueueMoveTrial(session, actorId, move))
					continue;

				if (session.PreviewWorld.StateOf(actorId).ActionPoints < 0)
				{
					session.TryUndoLast();
					continue;
				}

				var score = ScoreTurn(session, actorId);
				session.TryUndoLast();

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
				TryEnqueueMoveTrial(session, actorId, bestMove);
			else if (bestAction is not null)
				TryEnqueueTrial(session, bestAction);
		}

		return session.Actions.Skip(enemyActionStart).ToList();
	}

	private static int ScoreTurn(BattleSimulation session, string actorId)
	{
		BattleOrchestrator.ApplyEndOfPhase(
			session.PreviewWorld,
			session.PreviewActorRuntimes.For(actorId),
			actorId);

		foreach (var _ in session.StepPreview(TurnPhases.Enemy - TurnPhases.Player)) { }

		var state = session.PreviewWorld.StateOf(actorId);
		if (!state.IsAlive)
			return int.MinValue;

		return state.MomentumLevel * MomentumWeight - state.ActionPoints * UnusedApPenalty;
	}

	private static bool TryEnqueueTrial(BattleSimulation session, IAction candidate) =>
		session.TryEnqueue(candidate);

	private static bool TryEnqueueMoveTrial(BattleSimulation session, string actorId, Option move) =>
		BattleOrchestrator.TryEnqueueMovePath(session, actorId, move);
}
