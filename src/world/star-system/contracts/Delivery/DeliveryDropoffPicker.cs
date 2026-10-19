using GrimSpace.Math;
using GrimSpace.World.StarSystem.Poi;

namespace GrimSpace.World.StarSystem.Contracts;

public static class DeliveryDropoffPicker
{
	public static (string PoiId, string FacilityId, string OperatorName) Pick(
		StarMap map,
		string contractId,
		string scope,
		IReadOnlySet<string> excludedPoiIds)
	{
		ArgumentNullException.ThrowIfNull(map);
		ArgumentException.ThrowIfNullOrEmpty(contractId);
		ArgumentException.ThrowIfNullOrEmpty(scope);
		ArgumentNullException.ThrowIfNull(excludedPoiIds);

		var candidates = CollectCandidates(map, excludedPoiIds);
		if (candidates.Count == 0)
			throw new InvalidOperationException(
				$"No delivery dropoff candidates for map seed {map.Seed} with {excludedPoiIds.Count} excluded POI(s).");

		var random = new StableRandom(
			StableSeedMixer.From(map.Seed).Add(scope).Add(contractId).Value);

		var poiIndex = (int)(random.NextDouble() * candidates.Count);
		var (poiId, facilityId, operators) = candidates[poiIndex];

		var operatorIndex = (int)(random.NextDouble() * operators.Count);
		var operatorName = operators[operatorIndex].Name;

		return (poiId, facilityId, operatorName);
	}

	private static List<(string PoiId, string FacilityId, IReadOnlyList<FacilityOperator> Operators)> CollectCandidates(
		StarMap map,
		IReadOnlySet<string> excludedPoiIds)
	{
		var candidates = new List<(string, string, IReadOnlyList<FacilityOperator>)>();

		foreach (var poi in map.PointsOfInterest.OrderBy(p => p.Id, StringComparer.Ordinal))
		{
			if (excludedPoiIds.Contains(poi.Id))
				continue;

			foreach (var facility in poi.Facilities.OrderBy(f => f.Id, StringComparer.Ordinal))
			{
				if (facility.Operators.Count == 0)
					continue;

				candidates.Add((poi.Id, facility.Id, facility.Operators));
			}
		}

		return candidates;
	}
}
