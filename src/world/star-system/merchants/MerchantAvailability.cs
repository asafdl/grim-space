namespace GrimSpace.World.StarSystem.Merchants;

public static class MerchantAvailability
{
	public const int T0_5Bar = StarSystemDangerProgression.CompletionsPerTier;

	public const int T1Bar = T0_5Bar * 2;
	public const int T1_5Bar = T0_5Bar * 3;
	public const int T2Bar = T0_5Bar * 4;

	public static EMerchantAvailabilityBand CurrentBand(StarMap map)
	{
		ArgumentNullException.ThrowIfNull(map);
		return BandForCompletedCount(StarSystemDangerProgression.EffectiveCompletedContractCount(map));
	}

	public static EMerchantAvailabilityBand BandForCompletedCount(int completedContractCount)
	{
		if (completedContractCount >= T2Bar)
			return EMerchantAvailabilityBand.T2;
		if (completedContractCount >= T1_5Bar)
			return EMerchantAvailabilityBand.T1_5;
		if (completedContractCount >= T1Bar)
			return EMerchantAvailabilityBand.T1;
		if (completedContractCount >= T0_5Bar)
			return EMerchantAvailabilityBand.T0_5;

		return EMerchantAvailabilityBand.T0;
	}
}
