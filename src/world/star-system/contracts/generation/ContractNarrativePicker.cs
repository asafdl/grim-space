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
		int slotIndex)
	{
		ArgumentNullException.ThrowIfNull(map);

		var eligible = EligibleEntries(kind);
		if (eligible.Count == 0)
			return Fallback(kind);

		var random = CreateRandom(map.Seed, tick, slotIndex, kind);
		var index = (int)(random.NextDouble() * eligible.Count);
		return eligible[index].ToNarrative();
	}

	private IReadOnlyList<ContractNarrativePoolEntry> EligibleEntries(EContractKind kind) =>
		_config.Entries
			.Where(entry =>
				entry.Kind == kind
				&& entry.Subtype == ContractNarrativeSubtype.Contract)
			.ToArray();

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
