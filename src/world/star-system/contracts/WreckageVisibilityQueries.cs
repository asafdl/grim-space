using GrimSpace.Math.Grid;
using GrimSpace.World.StarSystem.Contracts.Objectives;

namespace GrimSpace.World.StarSystem.Contracts;

public static class WreckageVisibilityQueries
{
	public const int MapPickRadius = 4;

	public sealed record VisibleWreck(string ContractId, WreckageObjective Objective);

	public static IReadOnlyList<VisibleWreck> VisibleForHolder(StarMap map, string holderUnitId)
	{
		ArgumentNullException.ThrowIfNull(map);
		ArgumentException.ThrowIfNullOrEmpty(holderUnitId);

		return map.ContractRegistry.ActiveFor(holderUnitId)
			.Select(active => active.Definition)
			.Where(contract => contract.Objective is WreckageObjective)
			.Where(contract =>
				!ContractFactory.IsWreckageObjectiveMet(contract.Id, map, holderUnitId))
			.Select(contract => new VisibleWreck(
				contract.Id,
				(WreckageObjective)contract.Objective))
			.ToArray();
	}

	public static bool TryPickAt(
		StarMap map,
		string holderUnitId,
		Coord point,
		out VisibleWreck wreck)
	{
		wreck = null!;
		ArgumentNullException.ThrowIfNull(map);
		ArgumentException.ThrowIfNullOrEmpty(holderUnitId);

		VisibleWreck? best = null;
		var bestDistance = long.MaxValue;
		foreach (var candidate in VisibleForHolder(map, holderUnitId))
		{
			var position = candidate.Objective.Position;
			var dx = point.X - position.X;
			var dz = point.Z - position.Z;
			var distanceSquared = (long)dx * dx + (long)dz * dz;
			var pickRadiusSquared = (long)MapPickRadius * MapPickRadius;
			if (distanceSquared > pickRadiusSquared || distanceSquared >= bestDistance)
				continue;

			bestDistance = distanceSquared;
			best = candidate;
		}

		if (best is null)
			return false;

		wreck = best;
		return true;
	}
}
