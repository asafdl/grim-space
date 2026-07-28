using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Ai;
using GrimSpace.Battle.World;
using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.Turn;
using GrimSpace.Battle.Units;
using GrimSpace.Core.Actions;
using GrimSpace.Core.Engine;

namespace GrimSpace.Battle.Ai;

public static class EnemySimulation
{
	private const int MomentumWeight = 1_000;
	private const int UnusedApPenalty = 100;
	private const int TimelineRefinementLimit = 8;
	private const int TimelineRefinementSlack = UnusedApPenalty;

	public static IReadOnlyList<IAction> BuildTurnActions(BattleSimulation session, Unit actor)
	{
		var actorId = actor.State.Id;
		var start = session.Actions.Count;
		var capabilities = Capabilities.For(actor.State.Type);
		var best = SearchBestTurn(session, actorId, capabilities);

		foreach (var action in best.Actions.Skip(session.Actions.Count))
			session.TryEnqueue(action);

		return session.Actions.Skip(start).ToList();
	}

	private static SearchFrame<BattleWorld, ActorRuntime> SearchBestTurn(
		BattleSimulation session,
		string actorId,
		IReadOnlyList<IActionDef<IAction, BattleWorld, ActorRuntime, IEffect<BattleWorld, ActorRuntime>>> capabilities)
	{
		var finalists = new List<(SearchFrame<BattleWorld, ActorRuntime> Frame, int HeuristicScore)>();
		var bestHeuristic = int.MinValue;

		foreach (var frame in session.Search(actorId, capabilities, BattleSearchVisit.ForCapabilities))
		{
			if (!IsTerminalFrame(frame, actorId, capabilities))
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
			return new SearchFrame<BattleWorld, ActorRuntime>(
				session.World.Fork(),
				session.Runtimes.Fork(),
				session.Actions.ToList(),
				0);
		}

		SearchFrame<BattleWorld, ActorRuntime>? best = null;
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

	private static IEnumerable<(SearchFrame<BattleWorld, ActorRuntime> Frame, int HeuristicScore)> SelectTimelineFinalists(
		List<(SearchFrame<BattleWorld, ActorRuntime> Frame, int HeuristicScore)> finalists,
		int bestHeuristic)
	{
		var cutoff = bestHeuristic - TimelineRefinementSlack;
		return finalists
			.Where(candidate => candidate.HeuristicScore >= cutoff)
			.OrderByDescending(candidate => candidate.HeuristicScore)
			.Take(TimelineRefinementLimit);
	}

	private static bool IsTerminalFrame(
		SearchFrame<BattleWorld, ActorRuntime> frame,
		string actorId,
		IReadOnlyList<IActionDef<IAction, BattleWorld, ActorRuntime, IEffect<BattleWorld, ActorRuntime>>> capabilities)
	{
		var state = frame.World.StateOf(actorId);
		if (state.ActionPoints == 0)
			return true;

		var runtime = frame.Runtimes.For(actorId);
		foreach (var def in capabilities)
		{
			foreach (var candidate in def.Discover(frame.World, runtime, actorId))
			{
				if (def.IsLegal(candidate, frame.World, runtime))
					return false;
			}
		}

		return true;
	}

	private static int ScoreHeuristic(
		SearchFrame<BattleWorld, ActorRuntime> frame,
		string actorId)
	{
		var world = frame.World.Fork();
		var runtimes = frame.Runtimes.Fork();
		ExecutionHelper.Apply(new EndOfPhaseAction(actorId), world, runtimes.For(actorId));

		var state = world.StateOf(actorId);
		if (!state.IsAlive)
			return int.MinValue;

		return state.MomentumLevel * MomentumWeight - state.ActionPoints * UnusedApPenalty;
	}

	private static int ScoreTimelineAdjustment(
		SearchFrame<BattleWorld, ActorRuntime> frame,
		string actorId,
		int heuristicScore)
	{
		var world = frame.World.Fork();
		var runtimes = frame.Runtimes.Fork();
		ExecutionHelper.Apply(new EndOfPhaseAction(actorId), world, runtimes.For(actorId));

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
