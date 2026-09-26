using GrimSpace.World.StarSystem.Resources;

namespace GrimSpace.World.StarSystem.Merchants;

public static class MerchantUpgradePricing
{
	// Tier index = current upgrade tier before purchase (0 → first upgrade, etc.).
	private static readonly (int Credits, int Scrap, int Cores)[] WeaponDamage =
	[
		(120, 280, 0),
		(660, 1560, 1),
		(2720, 6400, 2),
	];

	private static readonly (int Credits, int Scrap, int Cores)[] WeaponRange =
	[
		(110, 260, 0),
		(600, 1410, 1),
		(2400, 5600, 2),
	];

	private static readonly (int Credits, int Scrap, int Cores)[] ShieldMaxPerFace =
	[
		(110, 260, 0),
		(600, 1410, 1),
		(2400, 5600, 2),
	];

	private static readonly (int Credits, int Scrap, int Cores)[] HullMax =
	[
		(130, 300, 0),
		(690, 1620, 1),
		(2800, 6560, 2),
	];

	public static ResourceBundle WeaponDamageUpgrade(int currentTier) => Bundle(WeaponDamage[currentTier]);

	public static ResourceBundle WeaponRangeUpgrade(int currentTier) => Bundle(WeaponRange[currentTier]);

	public static ResourceBundle ShieldMaxUpgrade(int currentTier) => Bundle(ShieldMaxPerFace[currentTier]);

	public static ResourceBundle HullMaxUpgrade(int currentTier) => Bundle(HullMax[currentTier]);

	public static ResourceBundle FlakInstall(int installedFlakCount) =>
		installedFlakCount == 0
			? Bundle((140, 340, 0))
			: Bundle((170, 390, 0));

	public static ResourceBundle RailgunInstall() => WeaponDamageUpgrade(0);

	private static ResourceBundle Bundle((int Credits, int Scrap, int Cores) price) =>
		ResourceBundle.Create(
			(ResourceId.Credits, price.Credits),
			(ResourceId.ScrapAlloy, price.Scrap),
			(ResourceId.IndustrialCore, price.Cores));
}
