using GrimSpace.World.StarSystem.Areas;
using GrimSpace.World.StarSystem.Encounter;

namespace GrimSpace.World.StarSystem.Contracts;

public abstract record ContractCreateArgs;

public sealed record HuntCreateArgs(
	AreaPickerArgs SearchAreaPicker,
	EDangerLevel Danger,
	ContractNarrative Narrative,
	bool IsStoryObjective = false) : ContractCreateArgs;

public sealed record DeliveryCreateArgs(
	string PickupPoiId,
	EDangerLevel Danger,
	ContractNarrative Narrative,
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
