namespace GrimSpace.World.StarSystem.Merchants;

public static class MerchantSubtypeStock
{
	public static bool HasOffers(StarMap map, EMerchantSubtype subtype)
	{
		ArgumentNullException.ThrowIfNull(map);
		return subtype switch
		{
			EMerchantSubtype.Pilots => PilotRecruitment.RecruitableBackgroundIds.Count > 0,
			EMerchantSubtype.Ships => map.Market.ListRecruitmentOffers().Count > 0,
			_ => throw new ArgumentOutOfRangeException(nameof(subtype), subtype, null),
		};
	}
}
