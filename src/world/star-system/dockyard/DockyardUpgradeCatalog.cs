using GrimSpace.Units;
using GrimSpace.Units.Loadouts.Abilities;
using GrimSpace.World.StarSystem.Resources;

namespace GrimSpace.World.StarSystem.Dockyard;

public static class DockyardUpgradeCatalog
{
	public static IReadOnlyList<DockyardUpgradeOffer> OffersFor(ShipInstance ship)
	{
		ArgumentNullException.ThrowIfNull(ship);
		var offers = new List<DockyardUpgradeOffer>();
		if (TryCreateShieldOffer(ship.Spec, out var shieldOffer))
			offers.Add(shieldOffer);

		foreach (var installed in ship.Spec.InstalledAbilities)
		{
			if (TryCreateAbilityOffer(installed, out var abilityOffer))
				offers.Add(abilityOffer);
		}

		return offers;
	}

	private static bool TryCreateShieldOffer(ShipSpec spec, out DockyardUpgradeOffer offer)
	{
		offer = null!;
		if (spec.ShieldUpgradeTier >= DockyardUpgradeRules.MaxShieldUpgradeTier)
			return false;

		var tier = spec.ShieldUpgradeTier;
		offer = new DockyardUpgradeOffer(
			ShieldOfferId(tier),
			EDockyardUpgradeCategory.MaxShields,
			null,
			tier,
			null,
			ResourceBundle.Of(ResourceId.ScrapAlloy, DockyardUpgradeRules.ScrapForShieldUpgrade(tier)));
		return true;
	}

	private static bool TryCreateAbilityOffer(InstalledAbility installed, out DockyardUpgradeOffer offer)
	{
		offer = null!;
		if (!AbilityUpgradeCatalog.CanUpgrade(installed.Spec))
			return false;

		offer = new DockyardUpgradeOffer(
			AbilityOfferId(installed.Mount),
			EDockyardUpgradeCategory.Ability,
			installed.Mount,
			0,
			installed.Spec,
			ResourceBundle.Of(
				ResourceId.ScrapAlloy,
				DockyardUpgradeRules.ScrapForAbilityUpgrade(AbilitySpecUpgrade.Tier(installed.Spec))));
		return true;
	}

	internal static string ShieldOfferId(int requiredTier) => $"max-shields:tier-{requiredTier}";

	internal static string AbilityOfferId(AbilityMount mount) =>
		$"ability:{mount.Kind}:{mount.Facet}";
}
