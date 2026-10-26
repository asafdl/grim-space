using GrimSpace.Math;
using GrimSpace.World.StarSystem.Contracts;

namespace GrimSpace.World.StarSystem.Contracts.Generation;

public sealed class ContractNarrativePicker
{
	private readonly ContractNarrativePickerConfig _config;

	public ContractNarrativePicker(ContractNarrativePickerConfig? config = null) =>
		_config = config ?? new ContractNarrativePickerConfig();

	public ContractNarrative Pick(
		StarMap map,
		EContractKind kind,
		int tick,
		int slotIndex,
		IReadOnlyList<Contract>? supplementalBoardContracts = null)
	{
		if (TryPick(map, kind, tick, slotIndex, out var narrative, supplementalBoardContracts))
			return narrative;

		return Fallback(kind);
	}

	public bool TryPick(
		StarMap map,
		EContractKind kind,
		int tick,
		int slotIndex,
		out ContractNarrative narrative,
		IReadOnlyList<Contract>? supplementalBoardContracts = null)
	{
		ArgumentNullException.ThrowIfNull(map);

		var eligible = EligibleEntries(kind);
		if (eligible.Count == 0)
		{
			narrative = Fallback(kind);
			return !IsTitleInUse(map, narrative.Title, supplementalBoardContracts);
		}

		var unused = FilterUnused(map, eligible, supplementalBoardContracts);
		if (unused.Count == 0)
		{
			narrative = null!;
			return false;
		}

		var random = CreateRandom(map.Seed, tick, slotIndex, kind);
		var index = (int)(random.NextDouble() * unused.Count);
		narrative = unused[index].ToNarrative();
		return true;
	}

	private IReadOnlyList<ContractNarrativePoolEntry> EligibleEntries(EContractKind kind) =>
		_config.Entries
			.Where(entry =>
				entry.Kind == kind
				&& entry.Subtype == ContractNarrativeSubtype.Contract)
			.ToArray();

	private static IReadOnlyList<ContractNarrativePoolEntry> FilterUnused(
		StarMap map,
		IReadOnlyList<ContractNarrativePoolEntry> eligible,
		IReadOnlyList<Contract>? supplementalBoardContracts)
	{
		var used = CollectUsedTitles(map, supplementalBoardContracts);
		if (used.Count == 0)
			return eligible;

		return eligible
			.Where(entry => !used.Contains(entry.Title))
			.ToArray();
	}

	private static HashSet<string> CollectUsedTitles(
		StarMap map,
		IReadOnlyList<Contract>? supplementalBoardContracts)
	{
		var used = new HashSet<string>(StringComparer.Ordinal);
		foreach (var contract in map.ContractRegistry.All)
		{
			if (!IsPendingOrActive(map, contract.Id))
				continue;

			used.Add(contract.Narrative.Title);
		}

		if (supplementalBoardContracts is null)
			return used;

		foreach (var contract in supplementalBoardContracts)
			used.Add(contract.Narrative.Title);

		return used;
	}

	private static bool IsTitleInUse(
		StarMap map,
		string title,
		IReadOnlyList<Contract>? supplementalBoardContracts) =>
		CollectUsedTitles(map, supplementalBoardContracts).Contains(title);

	private static bool IsPendingOrActive(StarMap map, string contractId) =>
		map.ContractRegistry.IsPending(contractId)
		|| map.ContractRegistry.TryGetState(contractId, out var state)
		&& state.Status == EContractStatus.Active;

	private static ContractNarrative Fallback(EContractKind kind) =>
		kind switch
		{
			EContractKind.Hunt => ContractNarrative.ForHunt("Pirate Hunt"),
			EContractKind.Delivery => ContractNarrative.ForDelivery(
				"Supply Run",
				"Take this package to the marked facility. The recipient knows what it is. We have made a deliberate choice not to.",
				"Put it down gently. No, not there. There. No—fine. Payment is already someone else's problem."),
			EContractKind.Wreckage => new ContractNarrative(
				"Derelict Survey",
				"Scanners flagged wreckage in open space. Find it, inspect it, and file whatever report you can justify."),
			EContractKind.Lead => new ContractNarrative(
				"Quiet Opportunity",
				"Hey, if you're interested, I heard there's a sale going at {destination} for {merchant}."),
			_ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null),
		};

	private static StableRandom CreateRandom(
		int mapSeed,
		int tick,
		int slotIndex,
		EContractKind kind) =>
		new(StableSeedMixer.From(mapSeed)
			.Add(tick)
			.Add(slotIndex)
			.Add(kind.ToString())
			.Add("contract-narrative")
			.Value);
}
