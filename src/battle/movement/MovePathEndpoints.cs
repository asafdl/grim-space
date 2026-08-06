using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Ai;
using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.World;
using GrimSpace.Core.Actions;
using GrimSpace.Core.Dfs;
using GrimSpace.Core.Engine;
using GrimSpace.Math.Grid;

namespace GrimSpace.Battle.Movement;

/// <summary>
/// Move-preview consumer: projects ActionSearch frames into one preferred extension per cell.
/// </summary>
public static class MovePathEndpoints
{
	public static IReadOnlyList<MovePathSession> DiscoverExtensions(
		Simulation<BattleWorld, ActorRuntime> sim,
		string actorId)
	{
		var results = new Dictionary<Coord, DisplayCandidate>();

		foreach (var frame in ActionSearch.Run(
			sim,
			actorId,
			[MoveDef.Instance],
			BattleSearchVisit.ForMovePreview))
		{
			if (frame.Depth <= 0)
				continue;

			var path = frame.Runtimes.For(actorId).ActivePath;
			if (path is null)
				continue;

			var session = TrimExtension(path, frame.Depth);
			if (session is null)
				continue;

			session.CanEndPath = path.CanEnd(frame.World.StateOf(actorId).Stats.MinPathApCost);
			var actor = frame.World.StateOf(actorId);
			var candidate = new DisplayCandidate(
				session,
				actor.ActionPoints,
				actor.MomentumLevel);

			if (!results.TryGetValue(session.EndPosition, out var existing)
				|| PreferDisplay(candidate, existing))
				results[session.EndPosition] = candidate;
		}

		return results.Values
			.Select(candidate => candidate.Session)
			.OrderBy(session => session.EndPosition.X)
			.ThenBy(session => session.EndPosition.Y)
			.ThenBy(session => session.EndPosition.Z)
			.ThenBy(session => session.Cells.Count)
			.ToList();
	}

	private static MovePathSession? TrimExtension(MovePathSession livePath, int extensionStepCount)
	{
		var session = livePath.Clone();
		var drop = session.Steps.Count - extensionStepCount;
		if (drop > 0)
		{
			session.Steps.RemoveRange(0, drop);
			session.Cells.RemoveRange(0, drop);
		}

		return session.Steps.Count == 0 ? null : session;
	}

	private static bool PreferDisplay(DisplayCandidate candidate, DisplayCandidate existing) =>
		candidate.RemainingAp > existing.RemainingAp
		|| candidate.RemainingAp == existing.RemainingAp
			&& candidate.Momentum > existing.Momentum
		|| candidate.RemainingAp == existing.RemainingAp
			&& candidate.Momentum == existing.Momentum
			&& candidate.Session.Steps.Count < existing.Session.Steps.Count;

	private readonly record struct DisplayCandidate(
		MovePathSession Session,
		int RemainingAp,
		int Momentum);
}
