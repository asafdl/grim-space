using GrimSpace.World.StarSystem.Areas;
using GrimSpace.World.StarSystem.Contracts.Objectives;

namespace GrimSpace.World.StarSystem.Contracts;

public abstract record ContractCreateArgs;

public sealed record HuntCreateArgs(
	string IssuerPoiId,
	AreaPickerArgs SearchAreaPicker,
	HuntEncounterArgs Encounter,
	ContractTerms Terms,
	ContractNarrative Narrative,
	bool IsStoryObjective) : ContractCreateArgs;

public sealed record DeliveryCreateArgs(
	string IssuerPoiId,
	ContractTerms Terms,
	ContractNarrative Narrative,
	bool IsStoryObjective,
	string? DropoffPoiId = null,
	string? DropoffFacilityId = null,
	string? DropoffOperatorName = null) : ContractCreateArgs;

public sealed record WreckageCreateArgs(
	string IssuerPoiId,
	AreaPickerArgs SearchAreaPicker,
	WreckageOutcome Outcome,
	ContractTerms Terms,
	ContractNarrative Narrative,
	bool IsStoryObjective) : ContractCreateArgs;
