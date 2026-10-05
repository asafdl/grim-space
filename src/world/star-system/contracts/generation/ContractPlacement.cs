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
		IReadOnlyList<Contract>? supplementalBoardContracts = null)
	{
		ArgumentNullException.ThrowIfNull(map);

		var issuers = ListIssuerPoiIds(map, tick);
		if (issuers.Count == 0)
			return null;

		var issuerRandom = CreateRandom(map.Seed, tick, slotIndex, "contract-issuer");
		var issuerPoiId = PickIssuer(map, issuers, issuerRandom, supplementalBoardContracts);
		if (issuerPoiId is null)
			return null;

		var kindRandom = CreateRandom(map.Seed, tick, slotIndex, "contract-kind");
		var kind = PickKind(map, issuerPoiId, kindRandom, supplementalBoardContracts);
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
		IReadOnlyList<Contract>? supplementalBoardContracts)
	{
		var counts = CountPendingByIssuer(map, supplementalBoardContracts);
		var maxPerPoi = _config.MaxPendingPerIssuerPoi(issuers.Count);
		var eligible = issuers
			.Where(issuerId => counts.GetValueOrDefault(issuerId) < maxPerPoi)
			.ToArray();
		if (eligible.Length == 0)
			return null;

		var minimumCount = eligible.Min(issuerId => counts.GetValueOrDefault(issuerId));
		var leastLoaded = eligible
			.Where(issuerId => counts.GetValueOrDefault(issuerId) == minimumCount)
			.ToArray();
		return leastLoaded[(int)(random.NextDouble() * leastLoaded.Length)];
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

		ApplyIssuerKindDiversityWeights(
			ref huntWeight,
			ref deliveryWeight,
			ref wreckageWeight,
			CountPendingKindsAtIssuer(map, issuerPoiId, supplementalBoardContracts));

		return PickWeightedIndex(
			[huntWeight, deliveryWeight, wreckageWeight],
			random) switch
		{
			0 => EContractKind.Hunt,
			1 => EContractKind.Delivery,
			_ => EContractKind.Wreckage,
		};
	}

	private static void ApplyIssuerKindDiversityWeights(
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

	private static Dictionary<string, int> CountPendingByIssuer(
		StarMap map,
		IReadOnlyList<Contract>? supplementalBoardContracts)
	{
		var counts = new Dictionary<string, int>(StringComparer.Ordinal);
		foreach (var contract in GeneratedBoardOccupants(map))
			AccumulateIssuer(contract, counts);

		if (supplementalBoardContracts is not null)
		{
			foreach (var contract in supplementalBoardContracts)
				AccumulateIssuer(contract, counts);
		}

		return counts;
	}

	private static void AccumulateIssuer(Contract contract, Dictionary<string, int> counts)
	{
		if (contract.IssuerPoiId is not { } issuerPoiId)
			return;

		counts.TryGetValue(issuerPoiId, out var count);
		counts[issuerPoiId] = count + 1;
	}

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

	private static StableRandom CreateRandom(int mapSeed, int tick, int slotIndex, string stream) =>
		new(StableSeedMixer.From(mapSeed).Add(tick).Add(slotIndex).Add(stream).Value);

	private static int PickWeightedIndex(IReadOnlyList<float> weights, StableRandom random)
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

	private static bool HasContractsDesk(PointOfInterest poi) =>
		poi.Facilities.Any(facility =>
			facility.Operators.Any(operatorEntry => operatorEntry.Role == EFacilityOperatorRole.Contracts));
}
