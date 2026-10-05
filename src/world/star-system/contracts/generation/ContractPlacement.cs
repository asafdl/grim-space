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

	public Decision? Pick(
		StarMap map,
		int tick,
		int slotIndex,
		IReadOnlyDictionary<string, int>? supplementalPendingByIssuer = null,
		IReadOnlyList<Contract>? supplementalBoardContracts = null)
	{
		ArgumentNullException.ThrowIfNull(map);

		var issuers = ListIssuerPoiIds(map, tick);
		if (issuers.Count == 0)
			return null;

		var random = CreateRandom(map.Seed, tick, slotIndex);
		var issuerPoiId = PickIssuer(map, issuers, random, supplementalPendingByIssuer);
		if (issuerPoiId is null)
			return null;

		var kind = PickKind(map, issuerPoiId, random, supplementalBoardContracts);
		return new Decision(issuerPoiId, kind);
	}

	private IReadOnlyList<string> ListIssuerPoiIds(StarMap map, int tick)
	{
		ArgumentNullException.ThrowIfNull(map);

		return map.PointsOfInterest
			.Where(HasContractsDesk)
			.Select(poi => poi.Id)
			.Where(poiId => !map.ContractRegistry.IsIssuerGenerationCoolingDown(poiId, tick))
			.OrderBy(id => id, StringComparer.Ordinal)
			.ToArray();
	}

	private string? PickIssuer(
		StarMap map,
		IReadOnlyList<string> issuers,
		StableRandom random,
		IReadOnlyDictionary<string, int>? supplementalPendingByIssuer)
	{
		var counts = CountPendingByIssuer(map);
		if (supplementalPendingByIssuer is not null)
		{
			foreach (var (issuerId, pending) in supplementalPendingByIssuer)
			{
				counts.TryGetValue(issuerId, out var count);
				counts[issuerId] = count + pending;
			}
		}

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

	private EContractKind PickKind(
		StarMap map,
		string issuerPoiId,
		StableRandom random,
		IReadOnlyList<Contract>? supplementalBoardContracts)
	{
		var huntWeight = _config.HuntKindWeight;
		var deliveryWeight = _config.DeliveryKindWeight;
		var wreckageWeight = _config.WreckageKindWeight;

		ApplyMonolithicKindWeights(
			ref huntWeight,
			ref deliveryWeight,
			ref wreckageWeight,
			CountPendingKindsAtIssuer(map, issuerPoiId, supplementalBoardContracts));
		ApplyMonolithicKindWeights(
			ref huntWeight,
			ref deliveryWeight,
			ref wreckageWeight,
			CountPendingKinds(map, supplementalBoardContracts));

		return PickWeightedIndex(
			[huntWeight, deliveryWeight, wreckageWeight],
			random) switch
		{
			0 => EContractKind.Hunt,
			1 => EContractKind.Delivery,
			_ => EContractKind.Wreckage,
		};
	}

	private static void ApplyMonolithicKindWeights(
		ref float huntWeight,
		ref float deliveryWeight,
		ref float wreckageWeight,
		(int HuntCount, int DeliveryCount, int WreckageCount) counts)
	{
		var (huntCount, deliveryCount, wreckageCount) = counts;
		if (huntCount > 0 && deliveryCount == 0 && wreckageCount == 0)
		{
			huntWeight *= 0.5f;
			deliveryWeight *= 2f;
			wreckageWeight *= 2f;
		}
		else if (deliveryCount > 0 && huntCount == 0 && wreckageCount == 0)
		{
			huntWeight *= 2f;
			deliveryWeight *= 0.5f;
			wreckageWeight *= 2f;
		}
		else if (wreckageCount > 0 && huntCount == 0 && deliveryCount == 0)
		{
			huntWeight *= 2f;
			deliveryWeight *= 2f;
			wreckageWeight *= 0.5f;
		}
	}

	private static Dictionary<string, int> CountPendingByIssuer(StarMap map)
	{
		var counts = new Dictionary<string, int>(StringComparer.Ordinal);
		foreach (var contract in GeneratedBoardOccupants(map))
		{
			if (contract.IssuerPoiId is not { } issuerPoiId)
				continue;

			counts.TryGetValue(issuerPoiId, out var count);
			counts[issuerPoiId] = count + 1;
		}

		return counts;
	}

	private static (int HuntCount, int DeliveryCount, int WreckageCount) CountPendingKinds(
		StarMap map,
		IReadOnlyList<Contract>? supplementalBoardContracts) =>
		CountKinds(GeneratedBoardOccupants(map), supplementalBoardContracts, issuerPoiId: null);

	private static (int HuntCount, int DeliveryCount, int WreckageCount) CountPendingKindsAtIssuer(
		StarMap map,
		string issuerPoiId,
		IReadOnlyList<Contract>? supplementalBoardContracts) =>
		CountKinds(GeneratedBoardOccupants(map), supplementalBoardContracts, issuerPoiId);

	private static (int HuntCount, int DeliveryCount, int WreckageCount) CountKinds(
		IEnumerable<Contract> boardContracts,
		IReadOnlyList<Contract>? supplementalBoardContracts,
		string? issuerPoiId)
	{
		var huntCount = 0;
		var deliveryCount = 0;
		var wreckageCount = 0;
		foreach (var contract in boardContracts)
			AccumulateKind(contract, issuerPoiId, ref huntCount, ref deliveryCount, ref wreckageCount);

		if (supplementalBoardContracts is not null)
		{
			foreach (var contract in supplementalBoardContracts)
				AccumulateKind(contract, issuerPoiId, ref huntCount, ref deliveryCount, ref wreckageCount);
		}

		return (huntCount, deliveryCount, wreckageCount);
	}

	private static void AccumulateKind(
		Contract contract,
		string? issuerPoiId,
		ref int huntCount,
		ref int deliveryCount,
		ref int wreckageCount)
	{
		if (issuerPoiId is not null
			&& !string.Equals(contract.IssuerPoiId, issuerPoiId, StringComparison.Ordinal))
			return;

		switch (contract.Objective)
		{
			case HuntObjective:
				huntCount++;
				break;
			case DeliveryObjective:
				deliveryCount++;
				break;
			case WreckageObjective:
				wreckageCount++;
				break;
		}
	}

	private static IEnumerable<Contract> GeneratedBoardOccupants(StarMap map) =>
		map.ContractRegistry.All.Where(contract =>
			!contract.IsStoryObjective
			&& (map.ContractRegistry.IsPending(contract.Id)
				|| map.ContractRegistry.TryGetState(contract.Id, out var state)
				&& state.Status == EContractStatus.Active));

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
