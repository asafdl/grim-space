using GrimSpace.Battle;
using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Movement;
using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.World;
using GrimSpace.Core.Actions;
using GrimSpace.Core.Engine;
using GrimSpace.Math.Grid;

namespace GrimSpace.Tests.Movement;

/// <summary>
/// Independent move-endpoint discovery: replay prefix, then DFS move-only continuations.
/// Does not use <see cref="MoveOptionIndex.GetPaths"/>.
/// </summary>
internal static class MoveEndpointOracle
{
	public static IReadOnlySet<Coord> DiscoverEndpointPositions(
		BattleOrchestrator battle,
		IReadOnlyList<IAction> committedPrefix)
	{
		var actions = committedPrefix.ToList();
		if (!Replay(actions, battle))
			return new HashSet<Coord>();

		var endpoints = new HashSet<Coord>();
		Dfs(battle, actions, extensionMoves: 0, endpoints);
		return endpoints;
	}

	private static void Dfs(
		BattleOrchestrator battle,
		List<IAction> actions,
		int extensionMoves,
		HashSet<Coord> endpoints)
	{
		if (!Replay(actions, battle, out var sim))
			return;

		var actorId = battle.PlayerId;
		var path = sim.RuntimeFor(actorId).ActivePath;
		if (extensionMoves > 0 && path is not null && path.CanEnd())
			endpoints.Add(sim.StateOf<ActorState>(actorId).Position);

		var runtime = sim.RuntimeFor(actorId);
		var world = sim.World;
		foreach (var direction in Enum.GetValues<ESpatialOrientation>())
		{
			var step = MoveDef.Instance.Bind(actorId, direction);
			if (!MoveDef.Instance.IsLegal(step, world, runtime))
				continue;

			actions.Add(step);
			Dfs(battle, actions, extensionMoves + 1, endpoints);
			actions.RemoveAt(actions.Count - 1);
		}
	}

	private static bool Replay(IReadOnlyList<IAction> actions, BattleOrchestrator battle) =>
		Replay(actions, battle, out _);

	private static bool Replay(
		IReadOnlyList<IAction> actions,
		BattleOrchestrator battle,
		out Simulation<BattleWorld, ActorRuntime> sim)
	{
		sim = battle.Engine.CreateSimulation();
		foreach (var action in actions)
		{
			if (!sim.TryEnqueue(action))
				return false;
		}

		return true;
	}
}
