using GrimSpace.World.StarSystem.Resources;

namespace GrimSpace.World.StarSystem.Merchants;

public enum MerchantListingPricingKind
{
	FixedBundle,
	ShieldRechargePerPoint,
}

public readonly struct MerchantListingPricing
{
	public MerchantListingPricingKind Kind { get; }
	public ResourceBundle FixedCost { get; }
	public int ShieldRechargeCreditsPerPoint { get; }

	private MerchantListingPricing(
		MerchantListingPricingKind kind,
		ResourceBundle fixedCost,
		int shieldRechargeCreditsPerPoint)
	{
		Kind = kind;
		FixedCost = fixedCost;
		ShieldRechargeCreditsPerPoint = shieldRechargeCreditsPerPoint;
	}

	public static MerchantListingPricing Fixed(ResourceBundle cost) =>
		new(MerchantListingPricingKind.FixedBundle, cost, 0);

	public static MerchantListingPricing ShieldRecharge(int creditsPerPoint) =>
		new(
			MerchantListingPricingKind.ShieldRechargePerPoint,
			ResourceBundle.Empty,
			creditsPerPoint);
}
