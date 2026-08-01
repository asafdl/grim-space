using System.Diagnostics;
using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Ai;
using GrimSpace.Battle.World;
using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.Turn;
using GrimSpace.Battle.Units;
using GrimSpace.Battle.Weapons;
using GrimSpace.Core.Actions;
using GrimSpace.Core.Engine;
using GrimSpace.Core.Log;

namespace GrimSpace.Battle.Ai;

public static class EnemySimulation
{
	private const int MomentumWeight = 1_000;
	private const int UnusedApPenalty = 100;
	private const int RailgunHitBonus = 2_000;
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
		var visitedFrames = 0;
		var terminalFrames = 0;
		var scoredFrames = 0;
		var prunedFrames = 0;
		var dfsTimer = Stopwatch.StartNew();

		foreach (var frame in session.Search(actorId, capabilities, BattleSearchVisit.ForCapabilities))
		{
			visitedFrames++;
			if (!IsTerminalFrame(frame, actorId, capabilities))
				continue;

			terminalFrames++;
			var state = frame.World.StateOf(actorId);
			if (!state.IsAlive)
				continue;

			var upperBound = ScoreUpperBound(state);
			if (ShouldPruneTerminal(upperBound, bestHeuristic, finalists))
			{
				prunedFrames++;
				continue;
			}

			scoredFrames++;
			var heuristicScore = ScoreHeuristic(frame, actorId);
			if (heuristicScore == int.MinValue)
				continue;

			TryAddFinalist(finalists, frame, heuristicScore);
			if (heuristicScore > bestHeuristic)
				bestHeuristic = heuristicScore;
		}

		dfsTimer.Stop();

		if (finalists.Count == 0)
		{
			GameLog.Log(
				$"Enemy DFS ({actorId}): visited={visitedFrames} terminals={terminalFrames} "
				+ $"scored={scoredFrames} pruned={prunedFrames} finalists=0 "
				+ $"dfs={dfsTimer.Elapsed.TotalMilliseconds:F1}ms");
			return new SearchFrame<BattleWorld, ActorRuntime>(
				session.World.Fork(),
				session.Runtimes.Fork(),
				session.Actions.ToList(),
				0);
		}

		SearchFrame<BattleWorld, ActorRuntime>? best = null;
		var bestTotal = int.MinValue;
		var refinementTimer = Stopwatch.StartNew();

		foreach (var (frame, heuristicScore) in SelectTimelineFinalists(finalists, bestHeuristic))
		{
			var total = heuristicScore + ScoreTimelineAdjustment(frame, actorId, heuristicScore);
			if (total <= bestTotal)
				continue;

			bestTotal = total;
			best = frame;
		}

		refinementTimer.Stop();
		GameLog.Log(
			$"Enemy DFS ({actorId}): visited={visitedFrames} terminals={terminalFrames} "
			+ $"scored={scoredFrames} pruned={prunedFrames} finalists={finalists.Count} "
			+ $"dfs={dfsTimer.Elapsed.TotalMilliseconds:F1}ms "
			+ $"refinement={refinementTimer.Elapsed.TotalMilliseconds:F1}ms");

		return best ?? finalists.OrderByDescending(candidate => candidate.HeuristicScore).First().Frame;
	}

	private static int ScoreUpperBound(ActorState state) =>
		state.MomentumLevel * MomentumWeight
		- state.ActionPoints * UnusedApPenalty
		+ (state.RailgunRemaining > 0 ? RailgunHitBonus : 0);

	private static bool ShouldPruneTerminal(
		int upperBound,
		int bestHeuristic,
		List<(SearchFrame<BattleWorld, ActorRuntime> Frame, int HeuristicScore)> finalists)
	{
		if (bestHeuristic != int.MinValue && upperBound < bestHeuristic - TimelineRefinementSlack)
			return true;

		if (finalists.Count < TimelineRefinementLimit)
			return false;

		var worstFinalist = finalists.Min(candidate => candidate.HeuristicScore);
		return upperBound <= worstFinalist;
	}

	private static void TryAddFinalist(
		List<(SearchFrame<BattleWorld, ActorRuntime> Frame, int HeuristicScore)> finalists,
		SearchFrame<BattleWorld, ActorRuntime> frame,
		int heuristicScore)
	{
		if (finalists.Count < TimelineRefinementLimit)
		{
			finalists.Add((frame, heuristicScore));
			return;
		}

		var worstIndex = 0;
		for (var i = 1; i < finalists.Count; i++)
		{
			if (finalists[i].HeuristicScore < finalists[worstIndex].HeuristicScore)
				worstIndex = i;
		}

		if (heuristicScore <= finalists[worstIndex].HeuristicScore)
			return;

		finalists[worstIndex] = (frame, heuristicScore);
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

		var score = state.MomentumLevel * MomentumWeight - state.ActionPoints * UnusedApPenalty;
		if (FrameFiredRailgun(frame, actorId))
			score += RailgunHitBonus;

		return score;
	}

	private static bool FrameFiredRailgun(SearchFrame<BattleWorld, ActorRuntime> frame, string actorId) =>
		frame.Actions.Any(action => action is RailgunAction railgun && railgun.ActorId == actorId);

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
