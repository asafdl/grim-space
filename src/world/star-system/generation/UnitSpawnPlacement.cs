using GrimSpace.Math;
using GrimSpace.World.StarSystem.Poi;
using GrimSpace.World.StarSystem.Traffic;
using GrimSpace.World.StarSystem.Units;

namespace GrimSpace.World.StarSystem.Generation;

public static class UnitSpawnPlacement
{
	private enum CandidateKind
	{
		DockedReady,
		Working,
	}

	private sealed record Candidate(CandidateKind Kind, int LegIndex, string? WorkingPoiId);

	public sealed record Result(
		EPhase Phase,
		string DockedAtDockId,
		int ChoreIndex,
		int WorkTicksRemaining,
		string? WorkingPoiId);

	public static Result Resolve(
		int seed,
		string unitId,
		EType type,
		IReadOnlyList<string> choreDockIds,
		IReadOnlyDictionary<string, Dock> docksById,
		IReadOnlyDictionary<string, PointOfInterest> poiById,
		IReadOnlySet<string> poisWithSpawnedWorkers)
	{
		ArgumentException.ThrowIfNullOrEmpty(unitId);
		ArgumentNullException.ThrowIfNull(choreDockIds);
		if (choreDockIds.Count == 0)
			throw new ArgumentException("At least one chore destination is required.", nameof(choreDockIds));

		var candidates = BuildCandidates(
			type,
			choreDockIds,
			docksById,
			poiById,
			poisWithSpawnedWorkers);
		if (candidates.Count == 0)
			throw new InvalidOperationException($"No spawn placement candidates for unit '{unitId}'.");

		var random = new StableRandom(
			StableSeedMixer.From(seed)
				.Add("unit-placement")
				.Add(unitId)
				.Value);
		var chosen = candidates[(int)(random.NextDouble() * candidates.Count)];
		return Materialize(chosen, type, choreDockIds, poiById, random);
	}

	private static List<Candidate> BuildCandidates(
		EType type,
		IReadOnlyList<string> choreDockIds,
		IReadOnlyDictionary<string, Dock> docksById,
		IReadOnlyDictionary<string, PointOfInterest> poiById,
		IReadOnlySet<string> poisWithSpawnedWorkers)
	{
		var candidates = new List<Candidate>();
		var legCount = choreDockIds.Count;

		for (var legIndex = 0; legIndex < legCount; legIndex++)
		{
			var dockId = choreDockIds[legIndex];
			var poi = PoiForDock(dockId, docksById, poiById);

			candidates.Add(new Candidate(CandidateKind.DockedReady, legIndex, null));

			if (poi.HasTasks
				&& !poisWithSpawnedWorkers.Contains(poi.Id)
				&& TryGetWorkDuration(poi, type, out _))
			{
				candidates.Add(new Candidate(CandidateKind.Working, legIndex, poi.Id));
			}
		}

		return candidates;
	}

	private static Result Materialize(
		Candidate candidate,
		EType type,
		IReadOnlyList<string> choreDockIds,
		IReadOnlyDictionary<string, PointOfInterest> poiById,
		StableRandom random)
	{
		var legCount = choreDockIds.Count;
		var dockId = choreDockIds[candidate.LegIndex];
		var choreIndex = (candidate.LegIndex + 1) % legCount;

		return candidate.Kind switch
		{
			CandidateKind.DockedReady => new Result(
				EPhase.Docked,
				dockId,
				choreIndex,
				0,
				null),
			CandidateKind.Working => new Result(
				EPhase.Working,
				dockId,
				choreIndex,
				RandomWorkTicks(
					random,
					poiById[candidate.WorkingPoiId!].DurationTicks(type)),
				candidate.WorkingPoiId),
			_ => throw new InvalidOperationException($"Unknown spawn candidate kind '{candidate.Kind}'."),
		};
	}

	private static int RandomWorkTicks(StableRandom random, int duration) =>
		1 + (int)(random.NextDouble() * duration);

	private static PointOfInterest PoiForDock(
		string dockId,
		IReadOnlyDictionary<string, Dock> docksById,
		IReadOnlyDictionary<string, PointOfInterest> poiById)
	{
		var poiId = docksById[dockId].PoiId;
		return poiById[poiId];
	}

	private static bool TryGetWorkDuration(PointOfInterest poi, EType type, out int duration)
	{
		try
		{
			duration = poi.DurationTicks(type);
			return duration > 0;
		}
		catch (InvalidOperationException)
		{
			duration = 0;
			return false;
		}
	}
}
