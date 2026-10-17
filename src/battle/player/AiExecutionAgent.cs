using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Ai;
using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.Units;
using GrimSpace.Battle.World;
using GrimSpace.Core.Actions;
using GrimSpace.Core.Dfs;
using GrimSpace.Core.Engine;

namespace GrimSpace.Battle.Player;

public sealed class AiExecutionAgent
	: SimulationExecutionAgent<BattleWorld, ActorRuntime>,
		IActionSink
{
	private bool _committed;
	private bool _produceRequested;

	public new BattleSimulation Sim { get; private set; } = null!;

	public bool IsPlanning => _canWork && !_committed;

	protected override bool PublishOnActivate => false;

	protected override void ProduceActionsJob(Simulation<BattleWorld, ActorRuntime> simulation)
	{
		_committed = false;
		Sim = (BattleSimulation)simulation;
		_produceRequested = true;

		var jobCanWorkGeneration = CanWorkGeneration;
		_ = Task.Run(() =>
		{
			try
			{
				var actor = UnitRegistry.For(Sim.World).UnitOf(_actorId!);
				var capabilities = AiController.SearchCapabilities(actor.State);
				var actions = Runner.CalcActions(
					Sim,
					actor,
					capabilities,
					new SearchInput<BattleWorld, ActorRuntime>(
						BattleSearchVisit.ForCapabilities,
						ShouldExploreAction: EnemySearchInput.ShouldExploreAction),
					frames => SelectBest(Sim, actor.State.Id, frames));

				// Actions are already enqueued by Runner.CalcActions, just publish them
				Commit();
			}
			catch (Exception ex)
			{
				Fail(ex, jobCanWorkGeneration);
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
		var dfsTimer = System.Diagnostics.Stopwatch.StartNew();

		foreach (var frame in frames)
		{
			if (hitBranchPrefix is not null && !IsActionPrefixExtension(frame.Actions, hitBranchPrefix))
				break;

			visitedFrames++;

			var upperBound = EnemySearchInput.UpperBound(frame.World, actorId);
			if (bestScore != int.MinValue && upperBound < bestScore - AiController.TimelineRefinementSlack)
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
		if (finalists.Count < AiController.TimelineRefinementLimit)
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
		var cutoff = bestHeuristic - AiController.TimelineRefinementSlack;
		return finalists
			.Where(candidate => candidate.HeuristicScore >= cutoff)
			.OrderByDescending(candidate => candidate.HeuristicScore)
			.Take(AiController.TimelineRefinementLimit);
	}

	public bool Commit()
	{
		if (_committed || !_canWork || !_produceRequested)
			return false;

		if (!Sim.TryCommit(out var actions, out _))
			return false;

		_committed = true;
		Publish(actions);
		return true;
	}

	public bool TryEnqueue(IReadOnlyList<IAction> actions) => false;
	public bool Undo() => false;
}