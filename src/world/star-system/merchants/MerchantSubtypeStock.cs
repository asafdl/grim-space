using GrimSpace.Units;
using GrimSpace.Units.Enums;

namespace GrimSpace.World.StarSystem.Merchants;

public static class MerchantSubtypeStock
{
	public static bool HasOffers(StarMap map, EMerchantSubtype subtype)
	{
		ArgumentNullException.ThrowIfNull(map);
		return subtype switch
		{
			EMerchantSubtype.Ships => map.Market
				.ListRecruitmentOffers(MerchantCatalog.Tier(map))
				.Count > 0,
			EMerchantSubtype.Weapons => MerchantCatalog.ListFor(
				EMerchantCatalog.Weapons,
				QuoteShip(),
				map).Count > 0,
			EMerchantSubtype.ShipSupport => MerchantCatalog.ListFor(
				EMerchantCatalog.ShipSupport,
				QuoteShip(),
				map).Count > 0,
			_ => throw new ArgumentOutOfRangeException(nameof(subtype), subtype, null),
		};
	}

	private static ShipInstance QuoteShip() =>
		ShipInstance.FromCatalog("merchant-subtype-stock-quote", EType.Fighter);
}
