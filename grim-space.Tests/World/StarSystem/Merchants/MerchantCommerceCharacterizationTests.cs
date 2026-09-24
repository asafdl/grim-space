using GrimSpace.Battle.Encounter;
using GrimSpace.Run;
using GrimSpace.Units;
using GrimSpace.Units.Enums;
using GrimSpace.Units.Loadouts.Abilities;
using GrimSpace.Math.Grid;
using GrimSpace.World.StarSystem;
using GrimSpace.World.StarSystem.Actions;
using GrimSpace.World.StarSystem.Effects;
using GrimSpace.World.StarSystem.Generation;
using GrimSpace.World.StarSystem.Merchants;
using GrimSpace.World.StarSystem.Poi.Concrete;
using GrimSpace.World.StarSystem.Resources;
using GrimSpace.World.StarSystem.Runtime;
using GrimSpace.Tests.World.StarSystem.Poi;

namespace GrimSpace.Tests.World.StarSystem.Merchants;

[StarSystemTestSuite]
public sealed class MerchantCommerceCharacterizationTests
{
	private const string DockyardFacilityId = "poi-trade-dockyard";

	[Fact]
	public void FighterDockyard_ListsExpectedUpgradeAndSupportQuotes()
	{
		var ship = ShipInstance.FromCatalog("fighter-1", EType.Fighter);

		var offers = WeaponsCatalog.ListFor(ship);
		Assert.Single(offers, o => o.Category == EWeaponsOfferCategory.MaxShields);
		Assert.Equal(3, offers.Count(o => o.Category == EWeaponsOfferCategory.Ability));

		var shieldOffer = offers.Single(o => o.Category == EWeaponsOfferCategory.MaxShields);
		Assert.True(shieldOffer.Cost.TryGet(ResourceId.ScrapAlloy, out var shieldScrap));
		Assert.Equal(MerchantPricingRules.ScrapForShieldUpgrade(0), shieldScrap);

		var flakOfferId = WeaponsCatalog.AbilityOfferId(
			new AbilityMount(EAbilityKind.Flak, ESpatialOrientation.Port));
		Assert.True(WeaponsCatalog.TryQuote(flakOfferId, ship, out var flakCost));
		Assert.True(flakCost.TryGet(ResourceId.ScrapAlloy, out var flakScrap));
		Assert.Equal(MerchantPricingRules.ScrapForAbilityUpgrade(0), flakScrap);

		ship.HullPoints = 1;
		Assert.True(ShipSupportCatalog.TryQuoteHullRepair(ship, out var hullCost));
		Assert.True(hullCost.TryGet(ResourceId.Credits, out var hullCredits));
		Assert.True(hullCost.TryGet(ResourceId.ScrapAlloy, out var hullScrap));
		Assert.Equal(ShipSupportCatalog.HullRepairCreditCost, hullCredits);
		Assert.Equal(ShipSupportCatalog.HullRepairScrapCost, hullScrap);

		ship = ShipInstance.FromCatalog("fighter-2", EType.Fighter);
		ship.ShieldPoints.Fill(0);
		Assert.True(ShipSupportCatalog.TryQuoteShieldRecharge(ship, out var rechargeCost));
		Assert.True(rechargeCost.TryGet(ResourceId.Credits, out var rechargeCredits));
		Assert.Equal(
			ship.TotalMaxShieldPoints * ShipSupportCatalog.ShieldRechargeCreditsPerPoint,
			rechargeCredits);
	}

	[Fact]
	public void CreateNewRun_CommittedUpgradeMatchesRegistryAndBattleSpawn()
	{
		using var run = State.CreateNewRun(42);
		var shipId = run.PlayerParty.ShipIds[0];
		var ship = run.ShipRegistry.Get(shipId);
		var offerId = WeaponsCatalog.ListFor(ship).First().Id;
		SeedScrap(run.StarSystem.Map, 500);
		var before = ship.Clone();
		var action = new PurchaseWeaponsUpgradeAction(
			State.PlayerFleetUnitId,
			SupplySystemPlan.Copper.TradeHubPoiId,
			DockyardFacilityId,
			MapFacilityOperators.ShopOperatorName(run.StarSystem.Map),
			offerId,
			before);

		run.StarSystem.CommitSetup(action);

		var registryShip = run.ShipRegistry.Get(shipId);
		var playerFleet = run.StarSystem.Map.FleetRegistry.FleetOf(State.PlayerFleetUnitId);
		var encounter = EngagementBattleFactory.Create(
			[playerFleet],
			run.ShipRegistry,
			seed: 9,
			id: "characterization-engagement");

		var spawn = encounter.Spawns.Single(s => s.Ship.Id == shipId);
		Assert.Equal(registryShip.Spec.ShieldUpgradeTier, spawn.Ship.Spec.ShieldUpgradeTier);
		Assert.True(registryShip.Spec.MaxShieldPoints.Matches(spawn.Ship.Spec.MaxShieldPoints));
		Assert.Equal(registryShip.Spec.InstalledAbilities, spawn.Ship.Spec.InstalledAbilities);
		Assert.Equal(registryShip.HullPoints, spawn.Ship.HullPoints);
		Assert.True(registryShip.ShieldPoints.Matches(spawn.Ship.ShieldPoints));
		Assert.NotEqual(before.Spec.ShieldUpgradeTier, registryShip.Spec.ShieldUpgradeTier);
	}

	private static void SeedScrap(StarMap map, int amount)
	{
		var runtime = new ActorRuntime();
		new ChangeResourceEffect(TransactionSource.MerchantPurchase, ResourceBundle.Of(ResourceId.ScrapAlloy, amount))
			.Apply(map, runtime, "seed");
	}
}
