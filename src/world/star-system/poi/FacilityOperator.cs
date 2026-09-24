using GrimSpace.World.StarSystem.Merchants;

namespace GrimSpace.World.StarSystem.Poi;

public sealed record FacilityOperator(
	string Name,
	EFacilityOperatorRole Role,
	string SceneSlotId,
	EMerchantCatalog? MerchantCatalog = null);
