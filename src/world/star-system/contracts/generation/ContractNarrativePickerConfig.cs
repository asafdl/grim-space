using GrimSpace.World.StarSystem.Poi.Concrete;

namespace GrimSpace.World.StarSystem.Contracts.Generation;

public sealed class ContractNarrativePickerConfig
{
	public IReadOnlyList<ContractNarrativePoolEntry> Entries { get; init; } = DefaultEntries;

	public static IReadOnlyList<ContractNarrativePoolEntry> DefaultEntries =>
	[
		new(
			EContractKind.Hunt,
			"Pirate Hunt",
			"Pirate activity is reducing route efficiency. The Optimality decrees them as SCRAP!"),
		new(
			EContractKind.Hunt,
			"Raider Sweep",
			"Unregistered hulls are skimming convoy lanes. Clear them before the ledger turns red."),
		new(
			EContractKind.Delivery,
			"Supply Run",
			"Move this cargo to the marked drop-off. Standard rates apply.",
			"Thanks. Dock fees are still your problem."),
		new(
			EContractKind.Delivery,
			"Priority Manifest",
			"Time-sensitive freight. Drop it at the marked facility before the window closes.",
			"Manifest closed. Don't expect a bonus."),
		new(
			EContractKind.Hunt,
			"Command Sweep",
			"Hostiles near command authority space. Neutralize them before the next audit.",
			FacilitySlug: AdministrativeCore.ManagementFacilitySlug),
	];
}
