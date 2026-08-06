using System.Diagnostics;
using GrimSpace.Battle.Actions;
using GrimSpace.Battle.World;
using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.Units;
using GrimSpace.Core.Actions;
using GrimSpace.Core.Dfs;
using GrimSpace.Core.Engine;
using GrimSpace.Core.Log;

namespace GrimSpace.Battle.Ai;

public sealed class AiController : IExecutionAgent<BattleWorld, ActorRuntime, Unit>
{
	public static AiController Instance { get; } = new();

	private const int TimelineRefinementLimit = 8;
	private const int TimelineRefinementSlack = EnemySearchInput.TimelineRefinementSlack;

	public Task<IReadOnlyList<IAction>> GetActionsAsync(
		Unit actor,
		Func<BattleSimulation> createSim) =>
		Task.Run(() => Plan(createSim(), actor));

	public IReadOnlyList<IAction> Plan(BattleSimulation session, Unit actor) =>
		Runner.CalcActions(
			session,
			actor,
			new SearchInput<BattleWorld, ActorRuntime>(BattleSearchVisit.ForCapabilities),
			frames => SelectBest(session, actor.State.Id, frames));

	private static SearchFrame<BattleWorld, ActorRuntime> SelectBest(
		BattleSimulation session,
		string actorId,
		IEnumerable<SearchFrame<BattleWorld, ActorRuntime>> frames)
	{
		var searchStartDepth = session.Actions.Count;
		var finalists = new List<(SearchFrame<BattleWorld, ActorRuntime> Frame, int HeuristicScore)>();
		var bestHeuristic = int.MinValue;
		var bestScore = int.MinValue;
		var visitedFrames = 0;
		var scoredFrames = 0;
		var dfsTimer = Stopwatch.StartNew();

		foreach (var frame in frames)
		{
			visitedFrames++;

			var upperBound = EnemySearchInput.UpperBound(frame.World, actorId);
			if (bestScore != int.MinValue && upperBound < bestScore - TimelineRefinementSlack)
				frame.PruneChildren = true;

			if (!frame.World.StateOf(actorId).IsAlive)
				continue;

			var heuristicScore = EnemySearchInput.ScoreHeuristic(frame, session, actorId, searchStartDepth);
			if (heuristicScore == int.MinValue)
				continue;

			scoredFrames++;
			if (heuristicScore > bestScore)
				bestScore = heuristicScore;

			TryAddFinalist(finalists, frame, heuristicScore);
			if (heuristicScore > bestHeuristic)
				bestHeuristic = heuristicScore;
		}

		dfsTimer.Stop();

		if (finalists.Count == 0)
		{
			GameLog.Log(
				$"Enemy DFS ({actorId}): visited={visitedFrames} scored={scoredFrames} finalists=0 "
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
			$"Enemy DFS ({actorId}): visited={visitedFrames} scored={scoredFrames} finalists={finalists.Count} "
			+ $"dfs={dfsTimer.Elapsed.TotalMilliseconds:F1}ms "
			+ $"refinement={refinementTimer.Elapsed.TotalMilliseconds:F1}ms");

		return best ?? finalists.OrderByDescending(candidate => candidate.HeuristicScore).First().Frame;
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

	private static int ScoreTimelineAdjustment(
		SearchFrame<BattleWorld, ActorRuntime> frame,
		string actorId,
		int heuristicScore)
	{
		var world = frame.World.Fork();
		var runtimes = frame.Runtimes.Fork();
		ExecutionHelper.Apply(new EndOfPhaseAction(actorId), world, runtimes.For(actorId));

		var state = world.StateOf(actorId);
		if (!state.IsAlive)
			return int.MinValue - heuristicScore;

		var timelineScore = state.MomentumLevel * EnemySearchInput.MomentumWeight
			- state.ActionPoints * EnemySearchInput.UnusedApPenalty;
		return timelineScore - heuristicScore;
	}
}
