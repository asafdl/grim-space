using GrimSpace.World.StarSystem.Areas;

namespace GrimSpace.World.StarSystem.Contracts;

public abstract record ContractCreateArgs;

public sealed record HuntCreateArgs(
	string IssuerPoiId,
	AreaPickerArgs SearchAreaPicker,
	HuntEncounterArgs Encounter,
	ContractTerms Terms,
	ContractNarrative Narrative,
	bool IsStoryObjective) : ContractCreateArgs;

public sealed record DeliveryCreateArgs(string PickupPoiId) : ContractCreateArgs;
