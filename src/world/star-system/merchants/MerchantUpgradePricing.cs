using GrimSpace.World.StarSystem.Resources;

namespace GrimSpace.World.StarSystem.Merchants;

public static class MerchantUpgradePricing
{
	// Tier index = current upgrade tier before purchase (0 → first upgrade, etc.).
	// Tier-0 credits follow install > damage > hull > range > shield (5:4:3:2:1); higher tiers scale similarly.
	private static readonly (int Credits, int Scrap, int Cores)[] WeaponInstall =
	[
		(150, 350, 0),
		(180, 420, 0),
	];

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

	private static readonly (int Credits, int Scrap, int Cores)[] HullMax =
	[
		(90, 210, 0),
		(495, 1155, 1),
		(2040, 4760, 2),
	];

	public static ResourceBundle WeaponDamageUpgrade(int currentTier) => Bundle(WeaponDamage[currentTier]);

	public static ResourceBundle WeaponRangeUpgrade(int currentTier) => Bundle(WeaponRange[currentTier]);

	public static ResourceBundle ShieldMaxUpgrade(int currentTier) => Bundle(ShieldMaxPerFace[currentTier]);

	public static ResourceBundle HullMaxUpgrade(int currentTier) => Bundle(HullMax[currentTier]);

	public static ResourceBundle FlakInstall(int installedFlakCount) =>
		Bundle(WeaponInstall[installedFlakCount == 0 ? 0 : 1]);

	public static ResourceBundle RailgunInstall() => Bundle(WeaponInstall[0]);

	private static ResourceBundle Bundle((int Credits, int Scrap, int Cores) price) =>
		ResourceBundle.Create(
			(ResourceId.Credits, price.Credits),
			(ResourceId.ScrapAlloy, price.Scrap),
			(ResourceId.IndustrialCore, price.Cores));
}
