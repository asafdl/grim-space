using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.Units;
using GrimSpace.Battle.World;
using GrimSpace.Core.Actions;
using GrimSpace.Core.Dfs;
using GrimSpace.Core.Engine;

namespace GrimSpace.Battle.Ai;

public static class Runner
{
	public static IReadOnlyList<IAction> CalcActions(
		BattleSimulation session,
		Unit actor,
		IReadOnlyList<IActionDef<IAction, BattleWorld, ActorRuntime, IEffect<BattleWorld, ActorRuntime>>> capabilities,
		SearchInput<BattleWorld, ActorRuntime> searchInput,
		Func<IEnumerable<SearchFrame<BattleWorld, ActorRuntime>>, SearchFrame<BattleWorld, ActorRuntime>> selectBest)
	{
		var actorId = actor.State.Id;
		var start = session.Actions.Count;
		var chosen = selectBest(ActionSearch.Run(session, actorId, capabilities, searchInput));

		foreach (var action in chosen.Actions.Skip(start))
			session.TryEnqueue(action);

		return session.Actions.Skip(start).ToList();
	}
}
