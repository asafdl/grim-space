using GrimSpace.Battle.Board;
using GrimSpace.Battle.Runtime;
using GrimSpace.Core.Actions;
using GrimSpace.Core.Engine;

namespace GrimSpace.Tests.Actions;

/// <summary>
/// Enumerates actions the search DFS would attempt: Discover candidates, then IsLegal.
/// </summary>
internal static class LegalActionProbe
{
	public static IReadOnlyList<IAction> LegalActions(
		Simulation<BattleBoard, ActorSession> session,
		string actorId,
		IReadOnlyList<IActionDef<IAction, BattleBoard, ActorSession, IEffect<BattleBoard, ActorSession>>> defs)
	{
		var results = new List<IAction>();
		var world = session.World;
		var runtime = session.RuntimeFor(actorId);

		foreach (var def in defs)
		{
			foreach (var candidate in def.Discover(world, runtime, actorId))
			{
				if (def.IsLegal(candidate, world, runtime))
					results.Add(candidate);
			}
		}

		return results;
	}

	public static IReadOnlyList<IAction> LegalActions(
		Simulation<BattleBoard, ActorSession> session,
		string actorId,
		IActionDef<IAction, BattleBoard, ActorSession, IEffect<BattleBoard, ActorSession>> def) =>
		LegalActions(session, actorId, [def]);

	public static bool HasAnyLegal(
		Simulation<BattleBoard, ActorSession> session,
		string actorId,
		IActionDef<IAction, BattleBoard, ActorSession, IEffect<BattleBoard, ActorSession>> def) =>
		LegalActions(session, actorId, def).Count > 0;

	public static void AssertExhausted(
		Simulation<BattleBoard, ActorSession> session,
		string actorId,
		IActionDef<IAction, BattleBoard, ActorSession, IEffect<BattleBoard, ActorSession>> def,
		string? because = null)
	{
		var legal = LegalActions(session, actorId, def);
		Assert.True(
			legal.Count == 0,
			because ?? $"Expected no legal {def.GetType().Name} actions, found {legal.Count}.");
	}

	public static void EnqueueAll(
		Simulation<BattleBoard, ActorSession> session,
		IReadOnlyList<IAction> actions)
	{
		foreach (var action in actions)
			Assert.True(session.TryEnqueue(action), $"Failed to enqueue {action.GetType().Name}.");
	}
}
