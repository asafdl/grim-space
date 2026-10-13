using GrimSpace.World.StarSystem.Resources;

namespace GrimSpace.World.StarSystem.Merchants;

public sealed class MerchantListing
{
	public string Id { get; }
	public EMerchantCatalog Catalog { get; }
	public MerchantCatalog.Offering? Offering { get; }
	public EMerchantAvailabilityBand AvailabilityBand { get; }
	public bool AlwaysAvailable { get; }
	public MerchantListingPricing Pricing { get; private set; }

	internal MerchantListing(
		string id,
		EMerchantCatalog catalog,
		MerchantCatalog.Offering? offering,
		EMerchantAvailabilityBand availabilityBand,
		bool alwaysAvailable,
		MerchantListingPricing pricing)
	{
		ArgumentException.ThrowIfNullOrEmpty(id);
		if (pricing.Kind == MerchantListingPricingKind.FixedBundle && offering is null)
			throw new ArgumentException("Fixed-price lines require an offering.", nameof(offering));
		if (pricing.Kind == MerchantListingPricingKind.ShieldRechargePerPoint && offering is not null)
			throw new ArgumentException("Shield recharge rate lines must not set an offering.", nameof(offering));

		Id = id;
		Catalog = catalog;
		Offering = offering;
		AvailabilityBand = availabilityBand;
		AlwaysAvailable = alwaysAvailable;
		Pricing = pricing;
	}

	internal MerchantListing ForkClone() =>
		new(
			Id,
			Catalog,
			Offering,
			AvailabilityBand,
			AlwaysAvailable,
			Pricing.Kind == MerchantListingPricingKind.FixedBundle
				? MerchantListingPricing.Fixed(CloneBundle(Pricing.FixedCost))
				: MerchantListingPricing.ShieldRecharge(Pricing.ShieldRechargeCreditsPerPoint));

	public void SetPricing(MerchantListingPricing pricing) => Pricing = pricing;

	private static ResourceBundle CloneBundle(ResourceBundle bundle)
	{
		var entries = new Dictionary<ResourceId, int>();
		foreach (var (id, amount) in bundle)
			entries[id] = amount;
		return ResourceBundle.Create(entries);
	}
}
