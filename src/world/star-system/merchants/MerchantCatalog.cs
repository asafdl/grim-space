using GrimSpace.Math.Grid;
using GrimSpace.Units;
using GrimSpace.Units.Loadouts.Abilities;
using GrimSpace.World.StarSystem.Resources;

namespace GrimSpace.World.StarSystem.Merchants;

public static class MerchantCatalog
{
	private static readonly SheetEntry[] TieredInventory =
	[
		new(
			EMerchantAvailabilityBand.T0,
			EMerchantCatalog.ShipSupport,
			new Offering(Kind.UpgradeMaxShields, 1)),
		new(
			EMerchantAvailabilityBand.T0_5,
			EMerchantCatalog.Weapons,
			new Offering(Kind.UpgradeDamage, 1)),
		new(
			EMerchantAvailabilityBand.T1,
			EMerchantCatalog.ShipSupport,
			new Offering(Kind.UpgradeMaxShields, 2)),
		new(
			EMerchantAvailabilityBand.T1,
			EMerchantCatalog.Weapons,
			new Offering(Kind.UpgradeRange, 1)),
		new(
			EMerchantAvailabilityBand.T1,
			EMerchantCatalog.Weapons,
			new Offering(Kind.UpgradeDamage, 2)),
		new(
			EMerchantAvailabilityBand.T2,
			EMerchantCatalog.Weapons,
			new Offering(Kind.UpgradeRange, 2)),
		new(
			EMerchantAvailabilityBand.T2,
			EMerchantCatalog.Weapons,
			new Offering(Kind.UpgradeDamage, 3)),
		new(
			EMerchantAvailabilityBand.T2,
			EMerchantCatalog.ShipSupport,
			new Offering(Kind.UpgradeMaxShields, 3)),
	];

	public static EMerchantAvailabilityBand Tier(StarMap map) =>
		MerchantAvailability.CurrentBand(map);

	public enum Kind
	{
		UpgradeDamage,
		UpgradeRange,
		UpgradeMaxShields,
		RepairHull,
		RechargeAllShields,
		RechargeShieldFace,
	}

	public readonly record struct Offering(
		Kind Kind,
		int TargetLevel = 0);

	public readonly record struct Target(
		AbilityMount? Mount = null,
		ESpatialOrientation? Face = null);

	public sealed record Offer(
		Offering Offering,
		ResourceBundle Cost,
		IReadOnlyList<Target> Targets);

	public static IReadOnlyList<Offer> ListFor(
		EMerchantCatalog catalog,
		ShipInstance ship,
		EMerchantAvailabilityBand tier)
	{
		ArgumentNullException.ThrowIfNull(ship);
		var offers = TieredInventory
			.Where(entry => entry.Catalog == catalog && IncludesSheet(tier, entry.Sheet))
			.Select(entry => TryCreateUpgradeOffer(entry.Offering, ship))
			.OfType<Offer>()
			.ToList();

		if (catalog == EMerchantCatalog.Healing)
			offers.AddRange(HealingServiceCatalog.ListServicesFor(ship));

		return offers;
	}

	public static bool TryFind(
		EMerchantCatalog catalog,
		Offering key,
		Target target,
		ShipInstance ship,
		EMerchantAvailabilityBand tier,
		out Offer offer)
	{
		offer = ListFor(catalog, ship, tier).FirstOrDefault(candidate =>
			candidate.Offering == key && candidate.Targets.Contains(target))!;
		return offer is not null;
	}

	private static Offer? TryCreateUpgradeOffer(Offering offering, ShipInstance ship)
	{
		var targets = TargetsFor(offering, ship);
		if (targets.Length == 0)
			return null;

		var cost = offering.Kind switch
		{
			Kind.UpgradeDamage =>
				MerchantUpgradePricing.WeaponDamageUpgrade(offering.TargetLevel),
			Kind.UpgradeRange =>
				MerchantUpgradePricing.WeaponRangeUpgrade(offering.TargetLevel),
			Kind.UpgradeMaxShields =>
				MerchantUpgradePricing.ShieldMaxUpgrade(offering.TargetLevel),
			_ => throw new InvalidOperationException(
				$"Tiered merchant inventory cannot contain {offering.Kind}."),
		};

		return new Offer(offering, cost, targets);
	}

	private static Target[] TargetsFor(Offering offering, ShipInstance ship)
	{
		var candidates = offering.Kind switch
		{
			Kind.UpgradeDamage or Kind.UpgradeRange =>
				ship.Loadout.InstalledAbilities.Select(ability => new Target(ability.Mount)),
			Kind.UpgradeMaxShields =>
				Enum.GetValues<ESpatialOrientation>().Select(face => new Target(Face: face)),
			_ => [],
		};

		return candidates
			.Where(target => MerchantShipChanges.TryPrepareAfter(offering, target, ship, out _))
			.ToArray();
	}

	public static bool IncludesSheet(
		EMerchantAvailabilityBand current,
		EMerchantAvailabilityBand sheet) =>
		current switch
		{
			EMerchantAvailabilityBand.T0 => sheet == EMerchantAvailabilityBand.T0,
			EMerchantAvailabilityBand.T0_5 =>
				sheet is EMerchantAvailabilityBand.T0 or EMerchantAvailabilityBand.T0_5,
			EMerchantAvailabilityBand.T1 => sheet == EMerchantAvailabilityBand.T1,
			EMerchantAvailabilityBand.T1_5 =>
				sheet is EMerchantAvailabilityBand.T1 or EMerchantAvailabilityBand.T1_5,
			EMerchantAvailabilityBand.T2 => sheet == EMerchantAvailabilityBand.T2,
			_ => throw new ArgumentOutOfRangeException(nameof(current), current, null),
		};

	private readonly record struct SheetEntry(
		EMerchantAvailabilityBand Sheet,
		EMerchantCatalog Catalog,
		Offering Offering);
}
