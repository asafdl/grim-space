using GrimSpace.Units;
using GrimSpace.Units.Enums;

namespace GrimSpace.World.StarSystem.Merchants;

public static class MerchantSubtypeStock
{
	public static bool HasOffers(StarMap map, EMerchantSubtype subtype)
	{
		ArgumentNullException.ThrowIfNull(map);
		var tier = MerchantCatalog.Tier(map);
		return subtype switch
		{
			EMerchantSubtype.Ships => ShipRecruitmentCatalog.ListFor(tier).Count > 0,
			EMerchantSubtype.Weapons => MerchantCatalog.ListFor(
				EMerchantCatalog.Weapons,
				QuoteShip(),
				tier).Count > 0,
			EMerchantSubtype.ShipSupport => MerchantCatalog.ListFor(
				EMerchantCatalog.ShipSupport,
				QuoteShip(),
				tier).Count > 0,
			_ => throw new ArgumentOutOfRangeException(nameof(subtype), subtype, null),
		};
	}

	private static ShipInstance QuoteShip() =>
		ShipInstance.FromCatalog("merchant-subtype-stock-quote", EType.Fighter);
}
