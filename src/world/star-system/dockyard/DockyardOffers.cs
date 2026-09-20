using GrimSpace.Math.Grid;
using GrimSpace.Units;
using GrimSpace.Units.Loadouts.Abilities;
using GrimSpace.Units.Loadouts.Defenses;
using GrimSpace.World.StarSystem.Resources;

namespace GrimSpace.World.StarSystem.Dockyard;

public static class DockyardOffers
{
	public static IReadOnlyList<DockyardUpgradeOffer> ListFor(ShipInstance ship) =>
		DockyardUpgradeCatalog.OffersFor(ship);

	public static bool TryQuote(string offerId, ShipInstance ship, out ResourceBundle cost)
	{
		cost = ResourceBundle.Empty;
		if (!TryGetOffer(offerId, ship, out var offer))
			return false;

		cost = offer.Cost;
		return true;
	}

	public static bool TryApply(string offerId, ShipInstance ship, out ShipInstance after)
	{
		after = null!;
		if (!TryGetOffer(offerId, ship, out var offer) || !MatchesShip(offer, ship))
			return false;

		return offer.Category switch
		{
			EDockyardUpgradeCategory.MaxShields => TryApplyShieldUpgrade(ship, out after),
			EDockyardUpgradeCategory.Ability => TryApplyAbilityUpgrade(ship, offer, out after),
			_ => false,
		};
	}

	private static bool TryApplyShieldUpgrade(ShipInstance ship, out ShipInstance after)
	{
		after = null!;
		try
		{
			var updatedSpec = ship.Spec.WithUpgradedMaxShields();
			var shields = BumpCurrentShields(ship.ShieldPoints, ship.Spec.MaxShieldPoints, updatedSpec.MaxShieldPoints);
			after = new ShipInstance(ship.Id, updatedSpec, ship.HullPoints, shields);
			return true;
		}
		catch (InvalidOperationException)
		{
			return false;
		}
	}

	private static bool TryApplyAbilityUpgrade(ShipInstance ship, DockyardUpgradeOffer offer, out ShipInstance after)
	{
		after = null!;
		if (offer.Mount is not { } mount)
			return false;

		var installed = ship.Spec.InstalledAbilities.FirstOrDefault(a => a.Mount == mount);
		if (installed is null || !AbilityUpgradeCatalog.TryCreateUpgraded(installed.Spec, out var replacement))
			return false;

		var updatedSpec = ship.Spec.WithReplacedMount(mount, replacement);
		after = new ShipInstance(ship.Id, updatedSpec, ship.HullPoints, ship.ShieldPoints.Clone());
		return true;
	}

	private static FaceShieldPoints BumpCurrentShields(
		FaceShieldPoints current,
		FaceShieldPoints previousMax,
		FaceShieldPoints newMax)
	{
		var bumped = current.Clone();
		foreach (ESpatialOrientation face in Enum.GetValues<ESpatialOrientation>())
		{
			var delta = newMax[face] - previousMax[face];
			if (delta <= 0)
				continue;

			bumped[face] = System.Math.Min(bumped[face] + delta, newMax[face]);
		}

		return bumped;
	}

	private static bool TryGetOffer(string offerId, ShipInstance ship, out DockyardUpgradeOffer offer)
	{
		offer = DockyardUpgradeCatalog.OffersFor(ship)
			.FirstOrDefault(candidate => string.Equals(candidate.Id, offerId, StringComparison.Ordinal))!;
		return offer is not null;
	}

	private static bool MatchesShip(DockyardUpgradeOffer offer, ShipInstance ship) =>
		offer.Category switch
		{
			EDockyardUpgradeCategory.MaxShields =>
				ship.Spec.ShieldUpgradeTier == offer.RequiredShieldTier,
			EDockyardUpgradeCategory.Ability => MatchesAbilityOffer(offer, ship),
			_ => false,
		};

	private static bool MatchesAbilityOffer(DockyardUpgradeOffer offer, ShipInstance ship)
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
