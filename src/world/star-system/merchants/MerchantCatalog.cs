using GrimSpace.Math.Grid;
using GrimSpace.Units;
using GrimSpace.Units.Loadouts.Abilities;
using GrimSpace.Pilots;
using GrimSpace.World.StarSystem.PilotSkills;
using GrimSpace.World.StarSystem.Resources;

namespace GrimSpace.World.StarSystem.Merchants;

public static class MerchantCatalog
{
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
		EMerchantAvailabilityBand tier) =>
		ListFor(catalog, ship, StarMarketSeed.Create(), tier);

	public static IReadOnlyList<Offer> ListFor(
		EMerchantCatalog catalog,
		ShipInstance ship,
		StarMap map,
		IReadOnlyList<PilotSkillGrant>? strategicGrants = null)
	{
		ArgumentNullException.ThrowIfNull(map);
		return ListFor(catalog, ship, map.Market, Tier(map))
			.Select(offer => offer with
			{
				Cost = IsUpgrade(offer.Offering.Kind)
					? StrategicPilotSkillModifiers.ApplyUpgradeDiscount(map, offer.Cost, strategicGrants)
					: offer.Cost,
			})
			.ToArray();
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

	public static bool TryFind(
		EMerchantCatalog catalog,
		Offering key,
		Target target,
		ShipInstance ship,
		StarMap map,
		out Offer offer,
		IReadOnlyList<PilotSkillGrant>? strategicGrants = null)
	{
		ArgumentNullException.ThrowIfNull(map);
		offer = ListFor(catalog, ship, map, strategicGrants).FirstOrDefault(candidate =>
			candidate.Offering == key && candidate.Targets.Contains(target))!;
		return offer is not null;
	}

	internal static IReadOnlyList<Offer> ListFor(
		EMerchantCatalog catalog,
		ShipInstance ship,
		StarMarket market,
		EMerchantAvailabilityBand tier)
	{
		ArgumentNullException.ThrowIfNull(ship);
		ArgumentNullException.ThrowIfNull(market);
		var offers = market.MerchantListings
			.Where(listing => ListingAvailableAtTier(catalog, listing, tier))
			.Where(listing =>
				listing.Pricing.Kind != MerchantListingPricingKind.ShieldRechargePerPoint)
			.Select(listing => TryCreateOffer(listing, ship))
			.OfType<Offer>()
			.ToList();

		if (catalog == EMerchantCatalog.Healing)
		{
			offers.AddRange(ShieldRechargeOffers(ship, market));
		}

		return offers;
	}

	private static bool ListingAvailableAtTier(
		EMerchantCatalog catalog,
		MerchantListing listing,
		EMerchantAvailabilityBand tier) =>
		listing.Catalog == catalog
		&& (listing.AlwaysAvailable || IncludesSheet(tier, listing.AvailabilityBand));

	private static Offer? TryCreateOffer(MerchantListing listing, ShipInstance ship)
	{
		if (listing.Offering is not { } offering)
			return null;

		return offering.Kind switch
		{
			Kind.UpgradeDamage or Kind.UpgradeRange or Kind.UpgradeMaxShields =>
				TryCreateUpgradeOffer(offering, ship, listing.Pricing.FixedCost),
			Kind.RepairHull => TryCreateRepairOffer(offering, ship, listing.Pricing.FixedCost),
			_ => null,
		};
	}

	private static Offer? TryCreateUpgradeOffer(
		Offering offering,
		ShipInstance ship,
		ResourceBundle cost)
	{
		var targets = TargetsFor(offering, ship);
		if (targets.Length == 0)
			return null;

		return new Offer(offering, cost, targets);
	}

	private static Offer? TryCreateRepairOffer(
		Offering offering,
		ShipInstance ship,
		ResourceBundle cost)
	{
		if (!MerchantShipChanges.TryPrepareAfter(offering, default, ship, out _))
			return null;

		return new Offer(offering, cost, [default]);
	}

	private static IReadOnlyList<Offer> ShieldRechargeOffers(ShipInstance ship, StarMarket market)
	{
		var rate = ShieldRechargeRate(market);
		var offers = new List<Offer>();

		var rechargeAll = new Offering(Kind.RechargeAllShields);
		if (MerchantShipChanges.TryPrepareAfter(rechargeAll, default, ship, out _))
		{
			offers.Add(new Offer(
				rechargeAll,
				ResourceBundle.Of(ResourceId.Credits, ship.MissingShieldPoints * rate),
				[default]));
		}

		foreach (var face in Enum.GetValues<ESpatialOrientation>())
		{
			var target = new Target(Face: face);
			var rechargeFace = new Offering(Kind.RechargeShieldFace);
			if (!MerchantShipChanges.TryPrepareAfter(rechargeFace, target, ship, out _))
				continue;

			offers.Add(new Offer(
				rechargeFace,
				ResourceBundle.Of(ResourceId.Credits, ship.MissingShieldPointsOnFace(face) * rate),
				[target]));
		}

		return offers;
	}

	private static int ShieldRechargeRate(StarMarket market) =>
		market.MerchantListings
			.Single(listing =>
				listing.Pricing.Kind == MerchantListingPricingKind.ShieldRechargePerPoint)
			.Pricing.ShieldRechargeCreditsPerPoint;

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

	private static bool IsUpgrade(Kind kind) =>
		kind is Kind.UpgradeDamage or Kind.UpgradeRange or Kind.UpgradeMaxShields;

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
}
