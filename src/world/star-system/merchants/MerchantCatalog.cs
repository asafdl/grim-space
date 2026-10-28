using GrimSpace.Math.Grid;
using GrimSpace.Units;
using GrimSpace.World.StarSystem.Resources;

namespace GrimSpace.World.StarSystem.Merchants;

public static class MerchantCatalog
{
	public static EMerchantAvailabilityBand Tier(StarMap map) =>
		MerchantAvailability.CurrentBand(map);

	public enum Kind
	{
		RepairHull,
		RechargeAllShields,
		RechargeShieldFace,
		BuyFuel,
	}

	public readonly record struct Offering(Kind Kind);

	public readonly record struct Target(ESpatialOrientation? Face = null);

	public sealed record Offer(
		Offering Offering,
		ResourceBundle Cost,
		Target Target);

	public static IReadOnlyList<Offer> ListFor(
		EMerchantCatalog catalog,
		ShipInstance ship,
		EMerchantAvailabilityBand tier) =>
		ListFor(catalog, ship, StarMarketSeed.Create(), tier);

	public static IReadOnlyList<Offer> ListFor(
		EMerchantCatalog catalog,
		ShipInstance ship,
		StarMap map)
	{
		ArgumentNullException.ThrowIfNull(map);
		return ListFor(catalog, ship, map.Market, Tier(map));
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
			candidate.Offering == key && candidate.Target == target)!;
		return offer is not null;
	}

	public static bool TryFind(
		EMerchantCatalog catalog,
		Offering key,
		Target target,
		ShipInstance ship,
		StarMap map,
		out Offer offer)
	{
		ArgumentNullException.ThrowIfNull(map);
		offer = ListFor(catalog, ship, map).FirstOrDefault(candidate =>
			candidate.Offering == key && candidate.Target == target)!;
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
			Kind.RepairHull => TryCreateRepairOffer(offering, ship, listing.Pricing.FixedCost),
			Kind.BuyFuel => new Offer(offering, listing.Pricing.FixedCost, default),
			_ => null,
		};
	}

	private static Offer? TryCreateRepairOffer(
		Offering offering,
		ShipInstance ship,
		ResourceBundle cost)
	{
		if (!MerchantShipChanges.TryPrepareAfter(offering, default, ship, out _))
			return null;

		return new Offer(offering, cost, default);
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
				ResourceBundle.Of(
					ResourceId.Credits,
					ship.MissingShieldPoints * rate / 100),
				default));
		}

		foreach (var face in Enum.GetValues<ESpatialOrientation>())
		{
			var target = new Target(Face: face);
			var rechargeFace = new Offering(Kind.RechargeShieldFace);
			if (!MerchantShipChanges.TryPrepareAfter(rechargeFace, target, ship, out _))
				continue;

			offers.Add(new Offer(
				rechargeFace,
				ResourceBundle.Of(
					ResourceId.Credits,
					ship.MissingShieldPointsOnFace(face) * rate / 100),
				target));
		}

		return offers;
	}

	private static int ShieldRechargeRate(StarMarket market) =>
		market.MerchantListings
			.Single(listing =>
				listing.Pricing.Kind == MerchantListingPricingKind.ShieldRechargePerPoint)
			.Pricing.ShieldRechargeCreditsPerPoint;

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
