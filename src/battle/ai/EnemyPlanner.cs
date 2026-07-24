using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Board;
using GrimSpace.Battle.Planning;
using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.Turn;
using GrimSpace.Battle.Units;
using GrimSpace.Battle.Weapons;
using GrimSpace.Core.Actions;
using GrimSpace.Core.Engine;
using GrimSpace.Units.Enums;
using BattleSimulation = GrimSpace.Core.Engine.Simulation<
	GrimSpace.Battle.Board.BattleBoard,
	GrimSpace.Battle.Runtime.ActorSession>;

namespace GrimSpace.Battle.Ai;

public static class EnemyPlanner
{
	private const int MomentumWeight = 1_000;
	private const int UnusedApPenalty = 100;
	private const int TimelineRefinementLimit = 8;
	private const int TimelineRefinementSlack = UnusedApPenalty;

	private static readonly IActionDef<IAction, BattleBoard, ActorSession, IEffect<BattleBoard, ActorSession>>[] FighterWeaponDefs =
	[
		RailgunDef.Instance,
		MissileDef.For(EMissileMount.Fore, CombatConfig.ForeMissileMinRange),
		FlakDef.For(EFlakMount.Port),
		FlakDef.For(EFlakMount.Starboard),
	];

	public static IReadOnlyList<IAction> PlanTurn(BattleSimulation session, Unit actor)
	{
		// Preview may carry an EndOfPhase overlay from the caller; plan on action replay only.
		session.Reevaluate();

		var actorId = actor.State.Id;
		var start = session.Actions.Count;

		EnqueueGreedyWeapons(session, actorId, actor.State.Type);

		var best = SearchBestMove(session, actorId);

		foreach (var action in best.Actions.Skip(session.Actions.Count))
			session.TryEnqueue(action);

		return session.Actions.Skip(start).ToList();
	}

	private static void EnqueueGreedyWeapons(
		BattleSimulation session,
		string actorId,
		EType unitType)
	{
		if (unitType != EType.Fighter)
			return;

		foreach (var def in FighterWeaponDefs)
		{
			var board = session.PreviewWorld;
			var runtime = session.PreviewActorRuntimes.For(actorId);

			foreach (var action in def.Discover(board, runtime, actorId))
			{
				if (!def.IsLegal(action, board, runtime))
					continue;

				session.TryEnqueue(action);
				break;
			}
		}
	}

	private static SearchFrame<BattleBoard, ActorSession> SearchBestMove(
		BattleSimulation session,
		string actorId)
	{
		var finalists = new List<(SearchFrame<BattleBoard, ActorSession> Frame, int HeuristicScore)>();
		var bestHeuristic = int.MinValue;

		foreach (var frame in session.SearchMoves(actorId))
		{
			if (!IsTerminalMoveFrame(frame, actorId))
				continue;

			var heuristicScore = ScoreHeuristic(frame, actorId);
			if (heuristicScore == int.MinValue)
				continue;

			finalists.Add((frame, heuristicScore));
			if (heuristicScore > bestHeuristic)
				bestHeuristic = heuristicScore;
		}

		if (finalists.Count == 0)
		{
			return new SearchFrame<BattleBoard, ActorSession>(
				session.PreviewWorld.Fork(),
				session.PreviewActorRuntimes.Fork(),
				session.Actions.ToList(),
				0);
		}

		SearchFrame<BattleBoard, ActorSession>? best = null;
		var bestTotal = int.MinValue;

		foreach (var (frame, heuristicScore) in SelectTimelineFinalists(finalists, bestHeuristic))
		{
			var total = heuristicScore + ScoreTimelineAdjustment(frame, actorId, heuristicScore);
			if (total <= bestTotal)
				continue;

			bestTotal = total;
			best = frame;
		}

		return best ?? finalists[0].Frame;
	}

	private static IEnumerable<(SearchFrame<BattleBoard, ActorSession> Frame, int HeuristicScore)> SelectTimelineFinalists(
		List<(SearchFrame<BattleBoard, ActorSession> Frame, int HeuristicScore)> finalists,
		int bestHeuristic)
	{
		var cutoff = bestHeuristic - TimelineRefinementSlack;
		return finalists
			.Where(candidate => candidate.HeuristicScore >= cutoff)
			.OrderByDescending(candidate => candidate.HeuristicScore)
			.Take(TimelineRefinementLimit);
	}

	private static bool IsTerminalMoveFrame(
		SearchFrame<BattleBoard, ActorSession> frame,
		string actorId)
	{
		var state = frame.World.StateOf(actorId);
		if (state.ActionPoints == 0)
			return true;

		var runtime = frame.Runtimes.For(actorId);
		foreach (var candidate in MoveDef.Instance.Discover(frame.World, runtime, actorId))
		{
			if (MoveDef.Instance.IsLegal(candidate, frame.World, runtime))
				return false;
		}

		return true;
	}

	private static int ScoreHeuristic(
		SearchFrame<BattleBoard, ActorSession> frame,
		string actorId)
	{
		var world = frame.World.Fork();
		var runtimes = frame.Runtimes.Fork();

		BattleOrchestrator.ApplyEndOfPhase(world, runtimes.For(actorId), actorId);

		var state = world.StateOf(actorId);
		if (!state.IsAlive)
			return int.MinValue;

		return state.MomentumLevel * MomentumWeight - state.ActionPoints * UnusedApPenalty;
	}

	private static int ScoreTimelineAdjustment(
		SearchFrame<BattleBoard, ActorSession> frame,
		string actorId,
		int heuristicScore)
	{
		var world = frame.World.Fork();
		var runtimes = frame.Runtimes.Fork();

		BattleOrchestrator.ApplyEndOfPhase(world, runtimes.For(actorId), actorId);

		foreach (var _ in TimelineRunner.Step(
			world.Timeline,
			world,
			runtimes,
			TurnPhases.Enemy - TurnPhases.Player)) { }

		var state = world.StateOf(actorId);
		if (!state.IsAlive)
			return int.MinValue - heuristicScore;

		var timelineScore = state.MomentumLevel * MomentumWeight - state.ActionPoints * UnusedApPenalty;
		return timelineScore - heuristicScore;
	}
}
