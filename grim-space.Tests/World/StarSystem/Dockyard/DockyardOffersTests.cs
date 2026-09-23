using GrimSpace.Units;
using GrimSpace.Units.Enums;
using GrimSpace.Units.Loadouts.Abilities;
using GrimSpace.Math.Grid;
using GrimSpace.World.StarSystem.Dockyard;
using GrimSpace.World.StarSystem.Resources;

namespace GrimSpace.Tests.World.StarSystem.Dockyard;

[StarSystemTestSuite]
public sealed class DockyardOffersTests
{
	[Fact]
	public void ListFor_FighterIncludesShieldAndAbilityOffers()
	{
		var ship = ShipInstance.FromCatalog("fighter-1", EType.Fighter);
		var offers = DockyardOffers.ListFor(ship);

		Assert.Contains(offers, offer => offer.Category == EDockyardUpgradeCategory.MaxShields);
		Assert.Equal(3, offers.Count(offer => offer.Category == EDockyardUpgradeCategory.Ability));
	}

	[Fact]
	public void ListFor_AbilityOfferPriceIncreasesAfterUpgrade()
	{
		var ship = ShipInstance.FromCatalog("fighter-1", EType.Fighter);
		var offerId = DockyardUpgradeCatalog.AbilityOfferId(
			new AbilityMount(EAbilityKind.Flak, ESpatialOrientation.Port));
		Assert.True(DockyardOffers.TryQuote(offerId, ship, out var firstCost));
		Assert.True(DockyardOffers.TryApply(offerId, ship, out var upgraded));
		Assert.True(DockyardOffers.TryQuote(offerId, upgraded, out var secondCost));
		Assert.True(firstCost.TryGet(ResourceId.ScrapAlloy, out var firstScrap));
		Assert.True(secondCost.TryGet(ResourceId.ScrapAlloy, out var secondScrap));
		Assert.True(secondScrap > firstScrap);
	}

	[Fact]
	public void TryApply_ShieldUpgrade_IncreasesMaxAndCurrentShields()
	{
		var ship = ShipInstance.FromCatalog("fighter-1", EType.Fighter);
		var offer = DockyardOffers.ListFor(ship).Single(o => o.Category == EDockyardUpgradeCategory.MaxShields);
		var previousMax = ship.Spec.MaxShieldPoints[ESpatialOrientation.Forward];

		Assert.True(DockyardOffers.TryApply(offer.Id, ship, out var after));

		Assert.Equal(1, after.Spec.ShieldUpgradeTier);
		Assert.Equal(previousMax + 1, after.Spec.MaxShieldPoints[ESpatialOrientation.Forward]);
		Assert.Equal(previousMax + 1, after.ShieldPoints[ESpatialOrientation.Forward]);
	}

	[Fact]
	public void TryApply_ReplacesOnlyTargetMountAndBumpsUpgradeTier()
	{
		var ship = ShipInstance.FromCatalog("fighter-1", EType.Fighter);
		var offer = DockyardOffers.ListFor(ship)
			.First(o => o.Mount == new AbilityMount(EAbilityKind.Flak, ESpatialOrientation.Port));

		Assert.True(DockyardOffers.TryApply(offer.Id, ship, out var after));

		var port = after.Spec.InstalledAbilities.First(a => a.MountedOn == ESpatialOrientation.Port);
		Assert.IsType<FlakSpec>(port.Spec);
		Assert.Equal(2, ((FlakSpec)port.Spec).Damage);
		Assert.Equal(1, ((FlakSpec)port.Spec).UpgradeTier);

		var starboard = after.Spec.InstalledAbilities.First(a => a.MountedOn == ESpatialOrientation.Starboard);
		Assert.Equal(0, ((FlakSpec)starboard.Spec).UpgradeTier);
	}

	[Fact]
	public void TryQuote_UsesTierBasedAbilityPricing()
	{
		var ship = ShipInstance.FromCatalog("fighter-1", EType.Fighter);
		var offerId = DockyardUpgradeCatalog.AbilityOfferId(
			new AbilityMount(EAbilityKind.Flak, ESpatialOrientation.Port));
		Assert.True(DockyardOffers.TryQuote(offerId, ship, out var cost));
		Assert.True(cost.TryGet(ResourceId.ScrapAlloy, out var scrap));
		Assert.Equal(DockyardUpgradeRules.ScrapForAbilityUpgrade(0), scrap);
	}

	[Fact]
	public void ShipSpec_WithReplacedMount_UpdatesMountAndValidates()
	{
		var spec = ShipCatalog.DefaultFor(EType.Fighter);
		var mount = new AbilityMount(EAbilityKind.Flak, ESpatialOrientation.Port);
		var replacement = new FlakSpec(1, 9, 2, UpgradeTier: 2);

		var updated = spec.WithReplacedMount(mount, replacement);
		var port = updated.InstalledAbilities.First(a => a.MountedOn == ESpatialOrientation.Port);

		Assert.Equal(replacement, port.Spec);
	}
}
