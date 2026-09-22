using GrimSpace.Math;
using GrimSpace.World.StarSystem.Poi;

namespace GrimSpace.World.StarSystem.Contracts;

public static class DeliveryDropoffPicker
{
	public static (string PoiId, string FacilityId, string OperatorName) Pick(
		StarMap map,
		string excludeIssuerPoiId,
		string contractId)
	{
		ArgumentNullException.ThrowIfNull(map);
		ArgumentException.ThrowIfNullOrEmpty(excludeIssuerPoiId);
		ArgumentException.ThrowIfNullOrEmpty(contractId);

		var candidates = CollectCandidates(map, excludeIssuerPoiId);
		if (candidates.Count == 0)
			throw new InvalidOperationException(
				$"No delivery dropoff candidates for map seed {map.Seed} excluding issuer '{excludeIssuerPoiId}'.");

		var random = new StableRandom(
			StableSeedMixer.From(map.Seed).Add("delivery-dropoff").Add(contractId).Value);

		var poiIndex = (int)(random.NextDouble() * candidates.Count);
		var (poiId, facilityId, operators) = candidates[poiIndex];

		var operatorIndex = (int)(random.NextDouble() * operators.Count);
		var operatorName = operators[operatorIndex].Name;

		return (poiId, facilityId, operatorName);
	}

	private static List<(string PoiId, string FacilityId, IReadOnlyList<FacilityOperator> Operators)> CollectCandidates(
		StarMap map,
		string excludeIssuerPoiId)
	{
		var candidates = new List<(string, string, IReadOnlyList<FacilityOperator>)>();

		foreach (var poi in map.PointsOfInterest.OrderBy(p => p.Id, StringComparer.Ordinal))
		{
			if (poi.Id == excludeIssuerPoiId)
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
