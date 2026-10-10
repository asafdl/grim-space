using GrimSpace.Math;
using GrimSpace.World.StarSystem.Contracts.Objectives;

namespace GrimSpace.World.StarSystem.Contracts.Generation;

public sealed class ContractPlacement
{
	private readonly ContractPlacementConfig _config;

	public sealed record Decision(EContractKind Kind);
	public ContractPlacement(ContractPlacementConfig? config = null) =>
		_config = config ?? new ContractPlacementConfig();

	public Decision? Pick(
		StarMap map,
		int tick,
		int slotIndex,
		IReadOnlyList<Contract>? supplementalBoardContracts = null)
	{
		ArgumentNullException.ThrowIfNull(map);

		var kindRandom = CreateRandom(map.Seed, tick, slotIndex, "contract-kind");
		return new Decision(PickKind(map, kindRandom, supplementalBoardContracts));
	}

	private EContractKind PickKind(
		StarMap map,
		StableRandom random,
		IReadOnlyList<Contract>? supplementalBoardContracts)
	{
		var huntWeight = _config.HuntKindWeight;
		var deliveryWeight = _config.DeliveryKindWeight;
		var wreckageWeight = _config.WreckageKindWeight;

		ApplyKindDiversityWeights(
			ref huntWeight,
			ref deliveryWeight,
			ref wreckageWeight,
			CountKinds(GeneratedBoardOccupants(map), supplementalBoardContracts));

		return PickWeightedIndex(
			[huntWeight, deliveryWeight, wreckageWeight],
			random) switch
		{
			0 => EContractKind.Hunt,
			1 => EContractKind.Delivery,
			_ => EContractKind.Wreckage,
		};
	}

	private static void ApplyKindDiversityWeights(
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

	private static (int HuntCount, int DeliveryCount, int WreckageCount) CountKinds(
		IEnumerable<Contract> boardContracts,
		IReadOnlyList<Contract>? supplementalBoardContracts)
	{
		var huntCount = 0;
		var deliveryCount = 0;
		var wreckageCount = 0;
		foreach (var contract in boardContracts)
			AccumulateKind(contract, ref huntCount, ref deliveryCount, ref wreckageCount);

		if (supplementalBoardContracts is not null)
		{
			foreach (var contract in supplementalBoardContracts)
				AccumulateKind(contract, ref huntCount, ref deliveryCount, ref wreckageCount);
		}

		return (huntCount, deliveryCount, wreckageCount);
	}

	private static void AccumulateKind(
		Contract contract,
		ref int huntCount,
		ref int deliveryCount,
		ref int wreckageCount)
	{
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

}
