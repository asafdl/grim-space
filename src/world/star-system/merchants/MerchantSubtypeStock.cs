namespace GrimSpace.World.StarSystem.Merchants;

public static class MerchantSubtypeStock
{
	public static bool HasOffers(StarMap map, EMerchantSubtype subtype)
	{
		ArgumentNullException.ThrowIfNull(map);
		return subtype switch
		{
			EMerchantSubtype.Pilots => HasPilotRecruitmentStock(map),
			EMerchantSubtype.Ships => map.Market
				.ListRecruitmentOffers(MerchantCatalog.Tier(map))
				.Count > 0,
			_ => throw new ArgumentOutOfRangeException(nameof(subtype), subtype, null),
		};
	}

	private static bool HasPilotRecruitmentStock(StarMap map)
	{
		var tier = MerchantCatalog.Tier(map);
		return PilotRecruitment.ListUnlockedBackgrounds(tier).Count > 0;
	}
}
