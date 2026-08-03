using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.World;
using GrimSpace.Core.Actions;
using GrimSpace.Core.Engine;
using GrimSpace.Math.Grid;

namespace GrimSpace.Battle.Movement;

/// <summary>
/// Move-only endpoint discovery from a replayed committed prefix.
/// </summary>
internal static class MovePathDiscovery
{
	public static IReadOnlyList<MovePathSession> DiscoverExtensions(
		Simulation<BattleWorld, ActorRuntime> turnStartSim,
		string actorId,
		IReadOnlyList<IAction> committedPrefix)
	{
		var sim = turnStartSim.BranchAtTurnStart();
		foreach (var action in committedPrefix)
		{
			if (!sim.TryEnqueue(action))
				return [];
		}

		var baselinePathApSpent = sim.RuntimeFor(actorId).ActivePath?.PathApSpent ?? 0;
		var results = new Dictionary<Coord, MovePathSession>();
		Search(sim, actorId, [], baselinePathApSpent, results);
		return results.Values
			.OrderBy(session => session.EndPosition.X)
			.ThenBy(session => session.EndPosition.Y)
			.ThenBy(session => session.EndPosition.Z)
			.ThenBy(session => session.Cells.Count)
			.ToList();
	}

	private static void Search(
		Simulation<BattleWorld, ActorRuntime> sim,
		string actorId,
		List<MoveStepAction> extensionSteps,
		int baselinePathApSpent,
		Dictionary<Coord, MovePathSession> results)
	{
		var runtime = sim.RuntimeFor(actorId);
		var path = runtime.ActivePath;
		if (extensionSteps.Count > 0 && path is not null && path.CanEnd())
			TryAddResult(results, path, extensionSteps.Count, baselinePathApSpent);

		var world = sim.World;
		foreach (var direction in Enum.GetValues<ESpatialOrientation>())
		{
			var step = MoveDef.Instance.Bind(actorId, direction);
			if (!MoveDef.Instance.IsLegal(step, world, runtime))
				continue;

			var fork = sim.Branch();
			if (!fork.TryEnqueue(step))
				continue;

			extensionSteps.Add(step);
			Search(fork, actorId, extensionSteps, baselinePathApSpent, results);
			extensionSteps.RemoveAt(extensionSteps.Count - 1);
		}
	}

	private static void TryAddResult(
		Dictionary<Coord, MovePathSession> results,
		MovePathSession livePath,
		int extensionStepCount,
		int baselinePathApSpent)
	{
		var session = livePath.Clone();
		var drop = session.Steps.Count - extensionStepCount;
		if (drop > 0)
		{
			session.Steps.RemoveRange(0, drop);
			session.Cells.RemoveRange(0, drop);
		}

		if (session.Steps.Count == 0)
			return;

		if (!results.TryGetValue(session.EndPosition, out var existing)
			|| PreferExtension(session, existing, baselinePathApSpent))
			results[session.EndPosition] = session;
	}

	private static bool PreferExtension(
		MovePathSession candidate,
		MovePathSession existing,
		int baselinePathApSpent) =>
		candidate.Steps.Count < existing.Steps.Count
		|| candidate.Steps.Count == existing.Steps.Count
			&& candidate.ExtensionApCost(baselinePathApSpent) < existing.ExtensionApCost(baselinePathApSpent);
}
