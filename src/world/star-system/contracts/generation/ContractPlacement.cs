using GrimSpace.Math;
using GrimSpace.World.StarSystem.Contracts.Objectives;
using GrimSpace.World.StarSystem.Poi;

namespace GrimSpace.World.StarSystem.Contracts.Generation;

public sealed class ContractPlacement
{
	private readonly ContractPlacementConfig _config;

	public sealed record Decision(string IssuerPoiId, EContractKind Kind);
	public ContractPlacement(ContractPlacementConfig? config = null) =>
		_config = config ?? new ContractPlacementConfig();

	public Decision? Pick(StarMap map, int tick, int slotIndex)
	{
		ArgumentNullException.ThrowIfNull(map);

		var issuers = ListIssuerPoiIds(map);
		if (issuers.Count == 0)
			return null;

		var random = CreateRandom(map.Seed, tick, slotIndex);
		var issuerPoiId = PickIssuer(map, issuers, random);
		if (issuerPoiId is null)
			return null;

		var kind = PickKind(map, issuerPoiId, random);
		return new Decision(issuerPoiId, kind);
	}

	private IReadOnlyList<string> ListIssuerPoiIds(StarMap map)
	{
		ArgumentNullException.ThrowIfNull(map);

		return map.PointsOfInterest
			.Where(HasContractsDesk)
			.Select(poi => poi.Id)
			.OrderBy(id => id, StringComparer.Ordinal)
			.ToArray();
	}

	private string? PickIssuer(StarMap map, IReadOnlyList<string> issuers, StableRandom random)
	{
		var counts = CountPendingByIssuer(map);
		var maxPerPoi = _config.MaxPendingPerIssuerPoi(issuers.Count);
		var eligible = issuers
			.Where(issuerId => counts.GetValueOrDefault(issuerId) < maxPerPoi)
			.ToArray();
		if (eligible.Length == 0)
			return null;

		var weights = new double[eligible.Length];
		for (var i = 0; i < eligible.Length; i++)
		{
			var count = counts.GetValueOrDefault(eligible[i]);
			var denominator = 1 + count;
			weights[i] = 1.0 / (denominator * denominator);
		}

		return eligible[PickWeightedIndex(weights, random)];
	}

	private EContractKind PickKind(StarMap map, string issuerPoiId, StableRandom random)
	{
		var (huntCount, deliveryCount) = CountPendingKindsAtIssuer(map, issuerPoiId);
		var huntWeight = _config.HuntKindWeight;
		var deliveryWeight = _config.DeliveryKindWeight;

		if (huntCount > 0 && deliveryCount == 0)
		{
			huntWeight *= 0.5f;
			deliveryWeight *= 2f;
		}
		else if (deliveryCount > 0 && huntCount == 0)
		{
			huntWeight *= 2f;
			deliveryWeight *= 0.5f;
		}

		var weights = new[] { huntWeight, deliveryWeight };
		return PickWeightedIndex(weights, random) == 0 ? EContractKind.Hunt : EContractKind.Delivery;
	}

	private static Dictionary<string, int> CountPendingByIssuer(StarMap map)
	{
		var counts = new Dictionary<string, int>(StringComparer.Ordinal);
		foreach (var contract in map.ContractRegistry.Pending)
		{
			if (contract.IssuerPoiId is not { } issuerPoiId)
				continue;

			counts.TryGetValue(issuerPoiId, out var count);
			counts[issuerPoiId] = count + 1;
		}

		return counts;
	}

	private static (int HuntCount, int DeliveryCount) CountPendingKindsAtIssuer(StarMap map, string issuerPoiId)
	{
		var huntCount = 0;
		var deliveryCount = 0;
		foreach (var contract in map.ContractRegistry.Pending)
		{
			if (!string.Equals(contract.IssuerPoiId, issuerPoiId, StringComparison.Ordinal))
				continue;

			if (contract.Objective is HuntObjective)
				huntCount++;
			else if (contract.Objective is DeliveryObjective)
				deliveryCount++;
		}

		return (huntCount, deliveryCount);
	}

	private static StableRandom CreateRandom(int mapSeed, int tick, int slotIndex) =>
		new(StableSeedMixer.From(mapSeed).Add(tick).Add(slotIndex).Add("contract-placement").Value);

	private static int PickWeightedIndex(IReadOnlyList<double> weights, StableRandom random)
	{
		var total = 0.0;
		foreach (var weight in weights)
			total += weight;

		if (total <= 0)
			return 0;

		var roll = random.NextDouble() * total;
		var cumulative = 0.0;
		for (var i = 0; i < weights.Count; i++)
		{
			cumulative += weights[i];
			if (roll < cumulative)
				return i;
		}

		return weights.Count - 1;
	}

	private static int PickWeightedIndex(IReadOnlyList<float> weights, StableRandom random) =>
		PickWeightedIndex(weights.Select(weight => (double)weight).ToArray(), random);

	private static bool HasContractsDesk(PointOfInterest poi) =>
		poi.Facilities.Any(facility =>
			facility.Operators.Any(operatorEntry => operatorEntry.Role == EFacilityOperatorRole.Contracts));

	
}
