using GrimSpace.World.StarSystem.Resources;

namespace GrimSpace.World.StarSystem.Merchants;

public static class MerchantUpgradePricing
{
	private static readonly (int Credits, int Scrap, int Cores)[] WeaponDamage =
	[
		(120, 280, 0),
		(660, 1540, 1),
		(2720, 6340, 2),
	];

	private static readonly (int Credits, int Scrap, int Cores)[] WeaponRange =
	[
		(60, 140, 0),
		(330, 770, 1),
		(1360, 3170, 2),
	];

	private static readonly (int Credits, int Scrap, int Cores)[] ShieldMaxPerFace =
	[
		(30, 70, 0),
		(165, 385, 1),
		(680, 1590, 2),
	];

	public static ResourceBundle WeaponDamageUpgrade(int targetUpgradeTier) =>
		Bundle(WeaponDamage[TierIndex(targetUpgradeTier, WeaponDamage.Length)]);

	public static ResourceBundle WeaponRangeUpgrade(int targetUpgradeTier) =>
		Bundle(WeaponRange[TierIndex(targetUpgradeTier, WeaponRange.Length)]);

	public static ResourceBundle ShieldMaxUpgrade(int targetMaxShieldPoints) =>
		Bundle(ShieldMaxPerFace[TierIndex(targetMaxShieldPoints, ShieldMaxPerFace.Length)]);

	private static int TierIndex(int targetUpgradeTier, int tierCount)
	{
		if (targetUpgradeTier <= 0 || targetUpgradeTier > tierCount)
			throw new ArgumentOutOfRangeException(
				nameof(targetUpgradeTier),
				targetUpgradeTier,
				$"Upgrade tier must be between 1 and {tierCount}.");
		return targetUpgradeTier - 1;
	}

	private static ResourceBundle Bundle((int Credits, int Scrap, int Cores) price) =>
		ResourceBundle.Create(
			(ResourceId.Credits, price.Credits),
			(ResourceId.ScrapAlloy, price.Scrap),
			(ResourceId.IndustrialCore, price.Cores));
}
