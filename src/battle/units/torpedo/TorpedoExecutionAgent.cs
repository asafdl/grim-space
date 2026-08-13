using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.Units;
using GrimSpace.Battle.World;
using GrimSpace.Core.Actions;
using GrimSpace.Core.Dfs;
using GrimSpace.Core.Engine;
using GrimSpace.Units.Enums;

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
			[MoveDef.Instance],
			new SearchInput<BattleWorld, ActorRuntime>(BattleSearchVisit.ForMove),
			frames => SelectBest(session, actorId, target, frames));

		session.TryEnqueue(new FuelBurnAction(actorId));
		TryEnqueueLegalAbilities(session, actorId);

		return session.Actions.Skip(start).ToList();
	}

	private static void TryEnqueueLegalAbilities(BattleSimulation session, string actorId)
	{
		var runtime = session.RuntimeFor(actorId);
		foreach (var def in Capabilities.AbilitiesFor(EType.Torpedo))
		{
			foreach (var action in def.Discover(session.World, runtime, actorId))
			{
				if (def.IsLegal(action, session.World, runtime))
					session.TryEnqueue(action);
			}
		}
	}

	private static SearchFrame<BattleWorld, ActorRuntime> SelectBest(
		BattleSimulation session,
		string actorId,
		Unit? target,
		IEnumerable<SearchFrame<BattleWorld, ActorRuntime>> frames)
	{
		var searchStartDepth = session.Actions.Count;
		SearchFrame<BattleWorld, ActorRuntime>? best = null;
		TorpedoFrameRank bestRank = default;

		foreach (var frame in frames)
		{
			var rank = TorpedoSearchInput.RankFrame(frame, session, actorId, target, searchStartDepth);
			if (rank.Score == int.MinValue)
				continue;
			if (best is not null && rank.CompareTo(bestRank) <= 0)
				continue;

			bestRank = rank;
			best = frame;
		}

		return best ?? new SearchFrame<BattleWorld, ActorRuntime>(
			session.World.Fork(),
			session.Runtimes.Fork(),
			session.Actions.ToList(),
			0);
	}
}
