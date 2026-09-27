using GrimSpace.Math.Grid;
using GrimSpace.World.StarSystem.Contracts.Objectives;
using GrimSpace.World.StarSystem.Runtime;
using GrimSpace.World.StarSystem.Vision;

namespace GrimSpace.World.StarSystem.Contracts;

public static class WreckageVisibilityQueries
{
	public const int MapPickRadius = 4;

	public sealed record VisibleWreck(string ContractId, WreckageObjective Objective);

	public static IReadOnlyList<VisibleWreck> VisibleForHolder(
		StarMap map,
		string holderUnitId,
		Func<string, ActorRuntime> runtimeFor,
		float tickFraction)
	{
		ArgumentNullException.ThrowIfNull(map);
		ArgumentException.ThrowIfNullOrEmpty(holderUnitId);
		ArgumentNullException.ThrowIfNull(runtimeFor);

		if (!map.FleetRegistry.TryGet(holderUnitId, out var holder))
			return [];

		var observer = FleetPositionSampler.Sample(
			map, holder.State, runtimeFor(holderUnitId), tickFraction);
		var radiusSquared = holder.State.VisionRadius * holder.State.VisionRadius;

		return map.ContractRegistry.ActiveFor(holderUnitId)
			.Select(active => active.Definition)
			.Where(contract => contract.Objective is WreckageObjective)
			.Where(contract =>
				!ContractFactory.IsWreckageObjectiveMet(contract.Id, map, holderUnitId))
			.Where(contract =>
			{
				var position = ((WreckageObjective)contract.Objective).Position;
				var dx = observer.X - position.X;
				var dz = observer.Z - position.Z;
				return dx * dx + dz * dz <= radiusSquared;
			})
			.Select(contract => new VisibleWreck(
				contract.Id,
				(WreckageObjective)contract.Objective))
			.ToArray();
	}

	public static bool TryPickAt(
		StarMap map,
		string holderUnitId,
		Coord point,
		Func<string, ActorRuntime> runtimeFor,
		float tickFraction,
		out VisibleWreck wreck)
	{
		wreck = null!;
		ArgumentNullException.ThrowIfNull(map);
		ArgumentException.ThrowIfNullOrEmpty(holderUnitId);

		VisibleWreck? best = null;
		var bestDistance = long.MaxValue;
		foreach (var candidate in VisibleForHolder(map, holderUnitId, runtimeFor, tickFraction))
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
