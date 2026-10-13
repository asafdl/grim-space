using GrimSpace.Units.Enums;
using GrimSpace.World.StarSystem.Resources;

namespace GrimSpace.World.StarSystem.Merchants;

public static class StarMarketSeed
{
	public const int HullRepairCreditCost = 20;
	public const int HullRepairScrapCost = 20;
	public const int ShieldRechargeCreditsPerPoint = 10;
	public const int RepurposedMinerCreditCost = 300;
	public const int GunshipCreditCost = 800;
	public const int GunshipIndustrialCoreCost = 2;

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

	private static readonly SheetEntry[] TieredUpgradeInventory =
	[
		new(
			"upgrade.ship_support.shields.1",
			EMerchantAvailabilityBand.T0,
			EMerchantCatalog.ShipSupport,
			new MerchantCatalog.Offering(MerchantCatalog.Kind.UpgradeMaxShields, 1)),
		new(
			"upgrade.weapons.damage.1",
			EMerchantAvailabilityBand.T0_5,
			EMerchantCatalog.Weapons,
			new MerchantCatalog.Offering(MerchantCatalog.Kind.UpgradeDamage, 1)),
		new(
			"upgrade.ship_support.shields.2",
			EMerchantAvailabilityBand.T1,
			EMerchantCatalog.ShipSupport,
			new MerchantCatalog.Offering(MerchantCatalog.Kind.UpgradeMaxShields, 2)),
		new(
			"upgrade.weapons.range.1",
			EMerchantAvailabilityBand.T1,
			EMerchantCatalog.Weapons,
			new MerchantCatalog.Offering(MerchantCatalog.Kind.UpgradeRange, 1)),
		new(
			"upgrade.weapons.damage.2",
			EMerchantAvailabilityBand.T1,
			EMerchantCatalog.Weapons,
			new MerchantCatalog.Offering(MerchantCatalog.Kind.UpgradeDamage, 2)),
		new(
			"upgrade.weapons.range.2",
			EMerchantAvailabilityBand.T2,
			EMerchantCatalog.Weapons,
			new MerchantCatalog.Offering(MerchantCatalog.Kind.UpgradeRange, 2)),
		new(
			"upgrade.weapons.damage.3",
			EMerchantAvailabilityBand.T2,
			EMerchantCatalog.Weapons,
			new MerchantCatalog.Offering(MerchantCatalog.Kind.UpgradeDamage, 3)),
		new(
			"upgrade.ship_support.shields.3",
			EMerchantAvailabilityBand.T2,
			EMerchantCatalog.ShipSupport,
			new MerchantCatalog.Offering(MerchantCatalog.Kind.UpgradeMaxShields, 3)),
	];

	public static StarMarket Create()
	{
		var merchantListings = new List<MerchantListing>();
		foreach (var entry in TieredUpgradeInventory)
		{
			merchantListings.Add(new MerchantListing(
				entry.Id,
				entry.Catalog,
				entry.Offering,
				entry.Sheet,
				alwaysAvailable: false,
				MerchantListingPricing.Fixed(UpgradeCost(entry.Offering))));
		}

		merchantListings.Add(new MerchantListing(
			"healing.repair_hull",
			EMerchantCatalog.Healing,
			new MerchantCatalog.Offering(MerchantCatalog.Kind.RepairHull),
			EMerchantAvailabilityBand.T0,
			alwaysAvailable: true,
			MerchantListingPricing.Fixed(ResourceBundle.Create(
				(ResourceId.Credits, HullRepairCreditCost),
				(ResourceId.ScrapAlloy, HullRepairScrapCost)))));

		merchantListings.Add(new MerchantListing(
			"healing.shield_recharge_per_point",
			EMerchantCatalog.Healing,
			offering: null,
			EMerchantAvailabilityBand.T0,
			alwaysAvailable: true,
			MerchantListingPricing.ShieldRecharge(ShieldRechargeCreditsPerPoint)));

		var recruitmentListings = new List<RecruitmentListing>
		{
			new(
				"recruitment.repurposed_miner.t1",
				EType.RepurposedMiner,
				EShipGearTier.T0,
				EMerchantAvailabilityBand.T1,
				ResourceBundle.Of(ResourceId.Credits, RepurposedMinerCreditCost)),
			new(
				"recruitment.gunship.t2",
				EType.Gunship,
				EShipGearTier.T0,
				EMerchantAvailabilityBand.T2,
				ResourceBundle.Create(
					(ResourceId.Credits, GunshipCreditCost),
					(ResourceId.IndustrialCore, GunshipIndustrialCoreCost))),
		};

		return new StarMarket(merchantListings, recruitmentListings);
	}

	private static ResourceBundle UpgradeCost(MerchantCatalog.Offering offering) =>
		offering.Kind switch
		{
			MerchantCatalog.Kind.UpgradeDamage =>
				UpgradeBundle(WeaponDamage, offering.TargetLevel),
			MerchantCatalog.Kind.UpgradeRange =>
				UpgradeBundle(WeaponRange, offering.TargetLevel),
			MerchantCatalog.Kind.UpgradeMaxShields =>
				UpgradeBundle(ShieldMaxPerFace, offering.TargetLevel),
			_ => throw new InvalidOperationException(
				$"Tiered merchant inventory cannot contain {offering.Kind}."),
		};

	private static ResourceBundle UpgradeBundle(
		(int Credits, int Scrap, int Cores)[] table,
		int targetTier)
	{
		if (targetTier <= 0 || targetTier > table.Length)
			throw new ArgumentOutOfRangeException(
				nameof(targetTier),
				targetTier,
				$"Upgrade tier must be between 1 and {table.Length}.");

		var price = table[targetTier - 1];
		return ResourceBundle.Create(
			(ResourceId.Credits, price.Credits),
			(ResourceId.ScrapAlloy, price.Scrap),
			(ResourceId.IndustrialCore, price.Cores));
	}

	private readonly record struct SheetEntry(
		string Id,
		EMerchantAvailabilityBand Sheet,
		EMerchantCatalog Catalog,
		MerchantCatalog.Offering Offering);
}
