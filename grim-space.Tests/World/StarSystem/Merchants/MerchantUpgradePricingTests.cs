using GrimSpace.World.StarSystem.Merchants;
using GrimSpace.World.StarSystem.Resources;

namespace GrimSpace.Tests.World.StarSystem.Merchants;

public sealed class MerchantUpgradePricingTests
{
	[Theory]
	[InlineData(0)]
	[InlineData(1)]
	[InlineData(2)]
	public void StatUpgradeTiers_ReflectPowerOrder_OnCredits(int tier)
	{
		int damage = Credits(MerchantUpgradePricing.WeaponDamageUpgrade(tier));
		int hull = Credits(MerchantUpgradePricing.HullMaxUpgrade(tier));
		int range = Credits(MerchantUpgradePricing.WeaponRangeUpgrade(tier));
		int shield = Credits(MerchantUpgradePricing.ShieldMaxUpgrade(tier));

		Assert.True(damage > hull);
		Assert.True(hull > range);
		Assert.True(range > shield);
	}

	[Fact]
	public void Tier0_InstallPricedAboveDamage()
	{
		int install = Credits(MerchantUpgradePricing.RailgunInstall());
		int damage = Credits(MerchantUpgradePricing.WeaponDamageUpgrade(0));
		Assert.True(install > damage);
	}

	[Fact]
	public void WeaponInstall_PricedAboveFirstDamageUpgrade()
	{
		Assert.True(
			Credits(MerchantUpgradePricing.RailgunInstall())
			> Credits(MerchantUpgradePricing.WeaponDamageUpgrade(0)));
		Assert.True(
			Credits(MerchantUpgradePricing.FlakInstall(0))
			> Credits(MerchantUpgradePricing.WeaponDamageUpgrade(0)));
	}

	private static int Credits(ResourceBundle bundle)
	{
		Assert.True(bundle.TryGet(ResourceId.Credits, out var amount));
		return amount;
	}
}
