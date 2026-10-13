using GrimSpace.Units.Enums;

namespace GrimSpace.World.StarSystem.Merchants;

public sealed class StarMarket
{
	private readonly MerchantListing[] _merchantListings;
	private readonly RecruitmentListing[] _recruitmentListings;

	public IReadOnlyList<MerchantListing> MerchantListings => _merchantListings;
	public IReadOnlyList<RecruitmentListing> RecruitmentListings => _recruitmentListings;

	internal StarMarket(
		IReadOnlyList<MerchantListing> merchantListings,
		IReadOnlyList<RecruitmentListing> recruitmentListings)
	{
		ArgumentNullException.ThrowIfNull(merchantListings);
		ArgumentNullException.ThrowIfNull(recruitmentListings);
		_merchantListings = merchantListings.ToArray();
		_recruitmentListings = recruitmentListings.ToArray();
	}

	public StarMarket CloneForFork() =>
		new(
			_merchantListings.Select(listing => listing.ForkClone()).ToArray(),
			_recruitmentListings.Select(listing => listing.ForkClone()).ToArray());

	public IReadOnlyList<RecruitmentOffer> ListRecruitmentOffers(EMerchantAvailabilityBand tier) =>
		_recruitmentListings
			.Where(listing => MerchantCatalog.IncludesSheet(tier, listing.AvailabilityBand))
			.Select(listing => listing.ToOffer())
			.ToArray();

	public bool TryFindRecruitmentOffer(
		EType chassis,
		EShipGearTier gearTier,
		EMerchantAvailabilityBand tier,
		out RecruitmentOffer offer)
	{
		offer = ListRecruitmentOffers(tier).FirstOrDefault(candidate =>
			candidate.Chassis == chassis && candidate.GearTier == gearTier)!;
		return offer is not null;
	}
}
