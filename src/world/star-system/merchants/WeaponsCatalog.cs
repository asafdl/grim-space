using GrimSpace.Units;
using GrimSpace.Units.Loadouts.Abilities;
using GrimSpace.World.StarSystem.Resources;

namespace GrimSpace.World.StarSystem.Merchants;

public static class WeaponsCatalog
{
	public static IReadOnlyList<WeaponsMerchantOffer> ListFor(ShipInstance ship)
	{
		ArgumentNullException.ThrowIfNull(ship);
		var offers = new List<WeaponsMerchantOffer>();
		if (TryCreateShieldOffer(ship.Spec, out var shieldOffer))
			offers.Add(shieldOffer);

		foreach (var installed in ship.Spec.InstalledAbilities)
		{
			if (TryCreateAbilityOffer(installed, out var abilityOffer))
				offers.Add(abilityOffer);
		}

		return offers;
	}

	public static bool TryQuote(string offerId, ShipInstance ship, out ResourceBundle cost)
	{
		cost = ResourceBundle.Empty;
		if (!TryGetOffer(offerId, ship, out var offer) || !MatchesShip(offer, ship))
			return false;

		cost = offer.Cost;
		return true;
	}

	public static bool TryGetOffer(string offerId, ShipInstance ship, out WeaponsMerchantOffer offer)
	{
		offer = ListFor(ship)
			.FirstOrDefault(candidate => string.Equals(candidate.Id, offerId, StringComparison.Ordinal))!;
		return offer is not null;
	}

	internal static bool MatchesShip(WeaponsMerchantOffer offer, ShipInstance ship) =>
		offer.Category switch
		{
			EWeaponsOfferCategory.MaxShields =>
				ship.Spec.ShieldUpgradeTier == offer.RequiredShieldTier,
			EWeaponsOfferCategory.Ability => MatchesAbilityOffer(offer, ship),
			_ => false,
		};

	public static string ShieldOfferId(int requiredTier) => $"max-shields:tier-{requiredTier}";

	public static string AbilityOfferId(AbilityMount mount) =>
		$"ability:{mount.Kind}:{mount.Facet}";

	private static bool TryCreateShieldOffer(ShipSpec spec, out WeaponsMerchantOffer offer)
	{
		offer = null!;
		if (spec.ShieldUpgradeTier >= MerchantPricingRules.MaxShieldUpgradeTier)
			return false;

		var tier = spec.ShieldUpgradeTier;
		offer = new WeaponsMerchantOffer(
			ShieldOfferId(tier),
			EWeaponsOfferCategory.MaxShields,
			null,
			tier,
			null,
			ResourceBundle.Of(ResourceId.ScrapAlloy, MerchantPricingRules.ScrapForShieldUpgrade(tier)));
		return true;
	}

	private static bool TryCreateAbilityOffer(InstalledAbility installed, out WeaponsMerchantOffer offer)
	{
		offer = null!;
		if (!installed.Spec.CanUpgrade)
			return false;

		offer = new WeaponsMerchantOffer(
			AbilityOfferId(installed.Mount),
			EWeaponsOfferCategory.Ability,
			installed.Mount,
			0,
			installed.Spec,
			ResourceBundle.Of(
				ResourceId.ScrapAlloy,
				MerchantPricingRules.ScrapForAbilityUpgrade(installed.Spec.UpgradeTier)));
		return true;
	}

	private static bool MatchesAbilityOffer(WeaponsMerchantOffer offer, ShipInstance ship)
	{
		if (offer.Mount is not { } mount || offer.RequiredAbilitySpec is not { } required)
			return false;

		foreach (var installed in ship.Spec.InstalledAbilities)
		{
			if (installed.Mount != mount)
				continue;

			return installed.Spec.Equals(required);
		}

		return false;
	}
}
