using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Ai;
using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.World;
using GrimSpace.Core.Actions;
using GrimSpace.Core.Dfs;
using GrimSpace.Core.Engine;
using GrimSpace.Core.Log;
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
		var start = sim.StateOf<ActorState>(actorId);
		var origin = start.Position;
		var startAp = start.ActionPoints;
		var startMom = start.MomentumLevel;

		var results = new Dictionary<Coord, DisplayCandidate>();
		var dfsPositions = new HashSet<Coord>();
		var acceptedEnds = new HashSet<Coord>();
		var frames = 0;
		var skipDepth0 = 0;
		var skipNullPath = 0;
		var skipTrimEmpty = 0;

		foreach (var frame in ActionSearch.Run(
			sim,
			actorId,
			[MoveDef.Instance],
			BattleSearchVisit.ForMovePreview))
		{
			frames++;
			if (frame.Depth <= 0)
			{
				skipDepth0++;
				continue;
			}

			var pos = frame.World.StateOf(actorId).Position;
			dfsPositions.Add(pos);

			var path = frame.Runtimes.For(actorId).ActivePath;
			if (path is null)
			{
				skipNullPath++;
				continue;
			}

			var session = TrimExtension(path, frame.Depth);
			if (session is null)
			{
				skipTrimEmpty++;
				continue;
			}

			session.CanEndPath = path.CanEnd(frame.World.StateOf(actorId).Stats.MinPathApCost);
			var actor = frame.World.StateOf(actorId);
			var candidate = new DisplayCandidate(
				session,
				actor.ActionPoints,
				actor.MomentumLevel);

			acceptedEnds.Add(session.EndPosition);
			if (!results.TryGetValue(session.EndPosition, out var existing)
				|| PreferDisplay(candidate, existing))
				results[session.EndPosition] = candidate;
		}

		var paths = results.Values
			.Select(candidate => candidate.Session)
			.OrderBy(session => session.EndPosition.X)
			.ThenBy(session => session.EndPosition.Y)
			.ThenBy(session => session.EndPosition.Z)
			.ThenBy(session => session.Cells.Count)
			.ToList();

		LogDiscovery(
			origin,
			startAp,
			startMom,
			frames,
			skipDepth0,
			skipNullPath,
			skipTrimEmpty,
			dfsPositions,
			acceptedEnds,
			paths,
			sim.World,
			actorId);

		return paths;
	}

	private static void LogDiscovery(
		Coord origin,
		int ap,
		int momentum,
		int frames,
		int skipDepth0,
		int skipNullPath,
		int skipTrimEmpty,
		HashSet<Coord> dfsPositions,
		HashSet<Coord> acceptedEnds,
		IReadOnlyList<MovePathSession> paths,
		BattleWorld world,
		string actorId)
	{
		var canEnd = paths.Count(path => path.CanEndPath);
		var reachedButDropped = dfsPositions.Except(acceptedEnds).OrderBy(c => c.X).ThenBy(c => c.Y).ThenBy(c => c.Z).ToList();

		GameLog.Log(
			$"[move-preview] discover actor={origin} ap={ap} mom={momentum} "
			+ $"frames={frames} dfsCells={dfsPositions.Count} accepted={acceptedEnds.Count} "
			+ $"projected={paths.Count} canEnd={canEnd}");
		GameLog.Log(
			$"[move-preview] skips depth0={skipDepth0} nullPath={skipNullPath} trimEmpty={skipTrimEmpty} "
			+ $"reachedButDropped={reachedButDropped.Count}"
			+ (reachedButDropped.Count == 0
				? string.Empty
				: $" [{string.Join(" ", reachedButDropped.Take(12))}"
					+ (reachedButDropped.Count > 12 ? " …]" : "]")));

		// Mom0 + N AP ⇒ full manhattan ball radius N (origin excluded) on an empty unbounded grid.
		if (momentum == 0 && ap > 0)
		{
			var expected = ManhattanBall(origin, radius: ap, includeOrigin: false);
			var missing = expected.Except(acceptedEnds).OrderBy(c => c.X).ThenBy(c => c.Y).ThenBy(c => c.Z).ToList();
			var extra = acceptedEnds.Except(expected).OrderBy(c => c.X).ThenBy(c => c.Y).ThenBy(c => c.Z).ToList();
			GameLog.Log(
				$"[move-preview] vs mom0-ball(r={ap}): expected={expected.Count} "
				+ $"missing={missing.Count} extra={extra.Count}");

			if (missing.Count > 0)
			{
				foreach (var cell in missing.Take(16))
					LogMissingCell(cell, origin, world, actorId, dfsPositions);
				if (missing.Count > 16)
					GameLog.Log($"[move-preview] missing cells: … +{missing.Count - 16} more");
			}

			if (extra.Count > 0)
			{
				GameLog.Log(
					$"[move-preview] extra cells: {string.Join(" ", extra.Take(16))}"
					+ (extra.Count > 16 ? " …" : string.Empty));
			}
		}
	}

	private static void LogMissingCell(
		Coord cell,
		Coord origin,
		BattleWorld world,
		string actorId,
		HashSet<Coord> dfsPositions)
	{
		var delta = cell - origin;
		var occupants = QueryCellOccupants(cell, world, actorId);
		GameLog.Log(
			$"[move-preview] missing {cell} Δ({delta.X},{delta.Y},{delta.Z}) "
			+ $"inBounds={world.Grid.IsInBounds(cell)} "
			+ $"blockedCells={world.BlockedCells.Contains(cell)} "
			+ $"blockedForActor={world.BlockedFor(actorId).Contains(cell)} "
			+ $"dfsHit={dfsPositions.Contains(cell)} "
			+ $"occupants=[{occupants}]");
	}

	private static string QueryCellOccupants(Coord cell, BattleWorld world, string actorId)
	{
		var parts = new List<string>();

		foreach (var unit in world.UnitRegistry.All)
		{
			if (unit.State.Position != cell)
				continue;
			var self = unit.State.Id == actorId ? "self," : string.Empty;
			parts.Add($"unit:{unit.State.Id}({self}team={unit.Alliance.Team},alive={unit.State.IsAlive})");
		}

		foreach (var pair in world.NonUnits)
		{
			var nonUnit = pair.Value;
			if (!nonUnit.Cells.Contains(cell))
				continue;

			if (nonUnit is Hazard hazard)
			{
				var cheb = Chebyshev(cell, hazard.Center);
				parts.Add(
					$"hazard:{hazard.Id}(kind={hazard.Kind},passable={hazard.Passable},"
					+ $"actor={hazard.ActorId},center={hazard.Center},"
					+ $"cheb={cheb},cells={hazard.Cells.Count})");
			}
			else
			{
				parts.Add($"nonUnit:{nonUnit.Id}(actor={nonUnit.ActorId},cells={nonUnit.Cells.Count})");
			}
		}

		return parts.Count == 0 ? "none" : string.Join(" | ", parts);
	}

	private static int Chebyshev(Coord a, Coord b) =>
		System.Math.Max(
			System.Math.Max(System.Math.Abs(a.X - b.X), System.Math.Abs(a.Y - b.Y)),
			System.Math.Abs(a.Z - b.Z));

	private static HashSet<Coord> ManhattanBall(Coord origin, int radius, bool includeOrigin)
	{
		var cells = new HashSet<Coord>();
		for (var dx = -radius; dx <= radius; dx++)
		{
			for (var dy = -radius; dy <= radius; dy++)
			{
				for (var dz = -radius; dz <= radius; dz++)
				{
					var dist = System.Math.Abs(dx) + System.Math.Abs(dy) + System.Math.Abs(dz);
					if (dist > radius || (dist == 0 && !includeOrigin))
						continue;
					cells.Add(origin + new Coord(dx, dy, dz));
				}
			}
		}

		return cells;
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
