using GrimSpace.World.StarSystem.Areas;
using GrimSpace.World.StarSystem.Contracts.Objectives;
using GrimSpace.World.StarSystem.Encounter;
using GrimSpace.World.StarSystem.Merchants;

namespace GrimSpace.World.StarSystem.Contracts;

public abstract record ContractCreateArgs;

public sealed record HuntCreateArgs(
	AreaPickerArgs SearchAreaPicker,
	EDangerLevel Danger,
	ContractNarrative Narrative,
	bool IsStoryObjective = false) : ContractCreateArgs;

public sealed record DeliveryCreateArgs(
	EDangerLevel Danger,
	ContractNarrative Narrative,
	string? PickupPoiId = null,
	bool IsStoryObjective = false,
	string? DropoffPoiId = null,
	string? DropoffFacilityId = null,
	string? DropoffOperatorName = null,
	DeliveryGenerationConfig? Generation = null) : ContractCreateArgs;

public sealed record WreckageCreateArgs(
	AreaPickerArgs SearchAreaPicker,
	EDangerLevel Danger,
	ContractNarrative Narrative,
	bool IsStoryObjective = false) : ContractCreateArgs;

public sealed record LeadCreateArgs(
	ELeadKind Kind,
	ContractNarrative Narrative,
	int DurationTicks,
	double SpaceMeetingChance = 0.2,
	BlackMarketSaleDestination? Destination = null,
	EMerchantSubtype? MerchantSubtype = null) : ContractCreateArgs;
