using GrimSpace.World.StarSystem.Merchants;
using GrimSpace.World.StarSystem.Resources;

namespace GrimSpace.Tests.World.StarSystem.Merchants;

public sealed class MerchantUpgradePricingTests
{
	[Theory]
	[InlineData(1)]
	[InlineData(2)]
	[InlineData(3)]
	public void UpgradePrices_IncreaseWithTargetTier(int targetTier)
	{
		var damage = Credits(MerchantUpgradePricing.WeaponDamageUpgrade(targetTier));
		var range = Credits(MerchantUpgradePricing.WeaponRangeUpgrade(targetTier));
		var shield = Credits(MerchantUpgradePricing.ShieldMaxUpgrade(targetTier));

		Assert.True(damage > range);
		Assert.True(range > shield);
	}

	[Theory]
	[InlineData(0)]
	[InlineData(4)]
	public void UpgradePrices_RejectUnsupportedTargetTier(int targetTier)
	{
		Assert.Throws<ArgumentOutOfRangeException>(() =>
			MerchantUpgradePricing.WeaponDamageUpgrade(targetTier));
	}

	private static int Credits(ResourceBundle bundle)
	{
		Assert.True(bundle.TryGet(ResourceId.Credits, out var amount));
		return amount;
	}
}
