using System.Diagnostics;
using GrimSpace.Battle.Actions;
using GrimSpace.Battle.World;
using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.Units;
using GrimSpace.Core.Actions;
using GrimSpace.Core.Dfs;
using GrimSpace.Core.Engine;
using GrimSpace.Core.Log;
using GrimSpace.Units.Enums;

namespace GrimSpace.Battle.Ai;

public sealed class AiController : ExecutionAgent<BattleWorld, ActorRuntime>
{
	private const int TimelineRefinementLimit = 8;
	private const int TimelineRefinementSlack = EnemySearchInput.TimelineRefinementSlack;

	protected override void ProduceActionsJob(Simulation<BattleWorld, ActorRuntime> simulation)
	{
		var session = (BattleSimulation)simulation;
		var actor = UnitRegistry.For(session.World).UnitOf(_actorId!);
		_ = Task.Run(() =>
		{
			try
			{
				var start = session.Actions.Count;
				var capabilities = Capabilities.For(actor.State.Type)
					.Where(def => def is not SpawnPatrolDef)
					.ToArray();
				var actions = Runner.CalcActions(
					session,
					actor,
					capabilities,
					new SearchInput<BattleWorld, ActorRuntime>(BattleSearchVisit.ForCapabilities),
					frames => SelectBest(session, actor.State.Id, frames));
				if (actor.State.Type == EType.Carrier && TryAppendPatrolDeploy(session, actor))
					actions = session.Actions.Skip(start).ToList();

				_actions!.TrySetResult(actions);
			}
			catch (Exception ex)
			{
				_actions!.TrySetException(ex);
			}
		});
	}

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
		IReadOnlyList<IAction>? hitBranchPrefix = null;
		var dfsTimer = Stopwatch.StartNew();

		foreach (var frame in frames)
		{
			if (hitBranchPrefix is not null && !IsActionPrefixExtension(frame.Actions, hitBranchPrefix))
				break;

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

			if (hitBranchPrefix is null
				&& EnemySearchInput.HasDamageHit(session, frame.Actions, actorId, searchStartDepth))
				hitBranchPrefix = frame.Actions;
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

		foreach (var (frame, heuristicScore) in SelectTimelineFinalists(finalists, bestHeuristic))
		{
			if (heuristicScore <= bestTotal)
				continue;

			bestTotal = heuristicScore;
			best = frame;
		}

		GameLog.Log(
			$"Enemy DFS ({actorId}): visited={visitedFrames} scored={scoredFrames} finalists={finalists.Count} "
			+ $"dfs={dfsTimer.Elapsed.TotalMilliseconds:F1}ms");

		var selected = best ?? finalists.OrderByDescending(candidate => candidate.HeuristicScore).First().Frame;
		return selected;
	}

	private static bool IsActionPrefixExtension(
		IReadOnlyList<IAction> actions,
		IReadOnlyList<IAction> prefix)
	{
		if (actions.Count < prefix.Count)
			return false;

		for (var i = 0; i < prefix.Count; i++)
		{
			if (!ReferenceEquals(actions[i], prefix[i]))
				return false;
		}

		return true;
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

	private static bool TryAppendPatrolDeploy(BattleSimulation session, Unit actor)
	{
		var action = SpawnPatrolDef.Instance.Bind(actor.State.Id);
		if (!SpawnPatrolDef.Instance.IsLegal(action, session.World, session.Runtimes.For(actor.State.Id)))
			return false;

		return session.TryEnqueue(action);
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
}
