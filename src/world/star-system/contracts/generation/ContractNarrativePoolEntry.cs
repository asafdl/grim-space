using GrimSpace.World.StarSystem.Contracts;

namespace GrimSpace.World.StarSystem.Contracts.Generation;

public enum ContractNarrativeSubtype
{
	Contract,
	DeliveryLeg,
}

/// <param name="FacilitySlug">
/// When set, entry is preferred when the issuer POI has
/// <see cref="Poi.Facility.ScopedId"/> for that slug; otherwise generic entries apply.
/// </param>
public sealed record ContractNarrativePoolEntry(
	EContractKind Kind,
	string Title,
	string Briefing,
	string TurnInDialog = "",
	string? FacilitySlug = null,
	ContractNarrativeSubtype Subtype = ContractNarrativeSubtype.Contract)
{
	public ContractNarrative ToNarrative() =>
		Subtype is not ContractNarrativeSubtype.Contract
			? throw new InvalidOperationException(
				$"Narrative subtype '{Subtype}' cannot be used as a contract narrative.")
			:
		Kind switch
		{
			EContractKind.Hunt => new ContractNarrative(Title, Briefing),
			EContractKind.Delivery => ContractNarrative.ForDelivery(Title, Briefing, TurnInDialog),
			EContractKind.Wreckage => new ContractNarrative(Title, Briefing),
			_ => throw new ArgumentOutOfRangeException(nameof(Kind), Kind, null),
		};
}
