using GrimSpace.Units.Enums;
using GrimSpace.World.StarSystem.Merchants;
using GrimSpace.World.StarSystem.Resources;

namespace GrimSpace.Run.Persistence;

public sealed record StarMarketDto(
	IReadOnlyList<MerchantListingDto> MerchantListings,
	IReadOnlyList<RecruitmentListingDto> RecruitmentListings);

public sealed record MerchantListingDto(
	string Id,
	EMerchantCatalog Catalog,
	MerchantCatalog.Kind? OfferingKind,
	int OfferingTargetLevel,
	EMerchantAvailabilityBand AvailabilityBand,
	bool AlwaysAvailable,
	MerchantListingPricingKind PricingKind,
	ResourceBundle FixedCost,
	int ShieldRechargeCreditsPerPoint);

public sealed record RecruitmentListingDto(
	string Id,
	EType Chassis,
	EShipGearTier GearTier,
	EMerchantAvailabilityBand AvailabilityBand,
	ResourceBundle Cost);
