using GrimSpace.World.StarSystem.Merchants;

namespace GrimSpace.World.StarSystem.Presentation.Facilities;

internal enum EMerchantPresentation
{
	Upgrades,
	Recruitment,
	PilotHiring,
}

internal static class MerchantSubtypeRouting
{
	public static EMerchantPresentation PresentationFor(EMerchantSubtype subtype) =>
		subtype switch
		{
			EMerchantSubtype.Weapons or EMerchantSubtype.ShipSupport =>
				EMerchantPresentation.Upgrades,
			EMerchantSubtype.Ships => EMerchantPresentation.Recruitment,
			EMerchantSubtype.Pilots => EMerchantPresentation.PilotHiring,
			_ => throw new ArgumentOutOfRangeException(nameof(subtype), subtype, null),
		};

	public static EMerchantCatalog CatalogFor(EMerchantSubtype subtype) =>
		subtype switch
		{
			EMerchantSubtype.Weapons => EMerchantCatalog.Weapons,
			EMerchantSubtype.ShipSupport => EMerchantCatalog.ShipSupport,
			EMerchantSubtype.Ships => EMerchantCatalog.Ships,
			EMerchantSubtype.Pilots => throw new InvalidOperationException(
				"Pilot hiring leads do not use merchant catalog quotes."),
			_ => throw new ArgumentOutOfRangeException(nameof(subtype), subtype, null),
		};
}
