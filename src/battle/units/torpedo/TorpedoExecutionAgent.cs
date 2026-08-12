using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.Units;
using GrimSpace.Battle.World;
using GrimSpace.Core.Actions;
using GrimSpace.Core.Dfs;
using GrimSpace.Core.Engine;

namespace GrimSpace.Battle.Ai;

public sealed class TorpedoExecutionAgent : ExecutionAgent<BattleWorld, ActorRuntime>
{
	protected override void ProduceActionsJob(Simulation<BattleWorld, ActorRuntime> simulation)
	{
		var session = (BattleSimulation)simulation;
		var actor = UnitRegistry.For(session.World).UnitOf(_actorId!);
		_ = Task.Run(() =>
		{
			try
			{
				_actions!.TrySetResult(Plan(actor, session));
			}
			catch (Exception ex)
			{
				_actions!.TrySetException(ex);
			}
		});
	}

	public IReadOnlyList<IAction> Plan(Unit actor, BattleSimulation session)
	{
		var start = session.Actions.Count;
		var actorId = actor.State.Id;
		var target = TorpedoSearchInput.BestReachableOpponent(session, actorId);

		Runner.CalcActions(
			session,
			actor,
			new SearchInput<BattleWorld, ActorRuntime>(BattleSearchVisit.ForMove),
			frames => SelectBest(session, actorId, target, frames));

		session.TryEnqueue(new FuelBurnAction(actorId));

		var detonate = DetonateDef.Instance.Bind(actorId);
		if (DetonateDef.Instance.IsLegal(detonate, session.World, session.RuntimeFor(actorId)))
			session.TryEnqueue(detonate);

		return session.Actions.Skip(start).ToList();
	}

	private static SearchFrame<BattleWorld, ActorRuntime> SelectBest(
		BattleSimulation session,
		string actorId,
		Unit? target,
		IEnumerable<SearchFrame<BattleWorld, ActorRuntime>> frames)
	{
		var searchStartDepth = session.Actions.Count;
		SearchFrame<BattleWorld, ActorRuntime>? best = null;
		var bestScore = int.MinValue;

		foreach (var frame in frames)
		{
			var score = TorpedoSearchInput.ScoreHeuristic(frame, session, actorId, target, searchStartDepth);
			if (score < bestScore)
				continue;

			bestScore = score;
			best = frame;
		}

		return best ?? new SearchFrame<BattleWorld, ActorRuntime>(
			session.World.Fork(),
			session.Runtimes.Fork(),
			session.Actions.ToList(),
			0);
	}
}
