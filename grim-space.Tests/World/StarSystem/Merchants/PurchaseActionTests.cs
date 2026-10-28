using GrimSpace.Core.Actions;
using GrimSpace.Core.Engine;
using GrimSpace.Math.Grid;
using GrimSpace.Run;
using GrimSpace.Units;
using GrimSpace.Units.Loadouts.Abilities;
using GrimSpace.World.StarSystem;
using GrimSpace.World.StarSystem.Actions;
using GrimSpace.World.StarSystem.Effects;
using GrimSpace.World.StarSystem.Generation;
using GrimSpace.World.StarSystem.Economy;
using GrimSpace.World.StarSystem.Merchants;
using GrimSpace.World.StarSystem.Poi;
using GrimSpace.World.StarSystem.Poi.Concrete;
using GrimSpace.World.StarSystem.PilotCapabilities;
using GrimSpace.World.StarSystem.Resources;
using GrimSpace.World.StarSystem.Runtime;
using GrimSpace.Tests.World.StarSystem.Poi;
using GrimSpace.Pilots;

namespace GrimSpace.Tests.World.StarSystem.Merchants;

[StarSystemTestSuite]
public sealed class PurchaseActionTests(StarMapFixture maps)
{
	private const string DockyardFacilityId = "poi-trade-dockyard";

	[Fact]
	public void Commit_DamagePack_UpgradesSelectedWeaponAndDebitsQuotedCost()
	{
		var (engine, unitId, ship, _) = MerchantPurchaseTestHarness.CreateEngine(maps);
		MerchantCatalogTestBands.SetBand(engine.World, EMerchantAvailabilityBand.T0_5);
		SeedResources(engine.World, credits: 5_000, scrap: 5_000, cores: 5);
		var offering = MerchantPurchaseTestHarness.ScrapDroneSwarmPortDamageUpgrade;
		var target = MerchantPurchaseTestHarness.ScrapDroneSwarmPortTarget;
		var before = ship.Clone();
		var initialCredits = engine.World.PlayerResources.GetBalance(ResourceId.Credits);
		var initialScrap = engine.World.PlayerResources.GetBalance(ResourceId.ScrapAlloy);

		var action = CreateAction(
			unitId,
			engine.World,
			EMerchantCatalog.Weapons,
			offering,
			target,
			before,
			engine);
		var pricedOffer = FetchMerchantCatalogActionDef.PriceCatalog(
				FetchMerchantCatalogActionDef.CreatePricingProbe(
				unitId,
				action.Endpoint,
				EMerchantCatalog.Weapons,
				before),
				engine.World,
				engine.ActorRuntimes.For(unitId))
			.Quote
			.Offers
			.Single(offer => offer.Offering == offering && offer.Targets.Contains(target));

		var records = engine.Commit(action);

		var purchase = Assert.Single(records.OfType<Record<MerchantShipPurchase>>()).Value;
		var upgraded = purchase.After.Loadout.InstalledAbilities.Single(
			ability => ability.Mount == target.Mount);
		Assert.Equal(1, upgraded.DamageUpgradeTier);
		Assert.Equal(
			initialCredits - Amount(pricedOffer.Cost, ResourceId.Credits),
			engine.World.PlayerResources.GetBalance(ResourceId.Credits));
		Assert.Equal(
			initialScrap - Amount(pricedOffer.Cost, ResourceId.ScrapAlloy),
			engine.World.PlayerResources.GetBalance(ResourceId.ScrapAlloy));
	}

	[Fact]
	public void Commit_DamagePack_AppliesStackedPilotDiscountToQuoteAndDebit()
	{
		var pilots = new RunPilotRegistry();
		for (var index = 0; index < 2; index++)
		{
			var pilot = PilotProgression
				.AddExperience(Pilot.CreateForShip($"fighter-{index}"), 450)
				.LearnPilotSkill(EPilotSkillId.DronesYearnForMines)
				.LearnPilotSkill(EPilotSkillId.IonizedPayload)
				.LearnPilotSkill(EPilotSkillId.IKnowABot);
			pilots.Register(pilot);
		}

		var (engine, unitId, ship, _) = MerchantPurchaseTestHarness.CreateEngine(
			maps,
			strategicGrants: pilots.QueryCapabilities(
				StarMapPilotCapabilityPipelineFactory.Supports));
		MerchantCatalogTestBands.SetBand(engine.World, EMerchantAvailabilityBand.T0_5);
		SeedResources(engine.World, credits: 5_000, scrap: 5_000, cores: 5);
		var offering = MerchantPurchaseTestHarness.ScrapDroneSwarmPortDamageUpgrade;
		var target = MerchantPurchaseTestHarness.ScrapDroneSwarmPortTarget;
		var before = ship.Clone();
		var initialCredits = engine.World.PlayerResources.GetBalance(ResourceId.Credits);
		var initialScrap = engine.World.PlayerResources.GetBalance(ResourceId.ScrapAlloy);

		var action = CreateAction(
			unitId,
			engine.World,
			EMerchantCatalog.Weapons,
			offering,
			target,
			before,
			engine);
		Assert.Equal(48, Amount(action.QuotedCost, ResourceId.Credits));
		Assert.Equal(112, Amount(action.QuotedCost, ResourceId.ScrapAlloy));

		engine.Commit(action);

		Assert.Equal(
			initialCredits - 48,
			engine.World.PlayerResources.GetBalance(ResourceId.Credits));
		Assert.Equal(
			initialScrap - 112,
			engine.World.PlayerResources.GetBalance(ResourceId.ScrapAlloy));
	}

	[Fact]
	public void TryEnqueue_DamagePack_RejectsWrongBand()
	{
		var (engine, unitId, ship, _) = MerchantPurchaseTestHarness.CreateEngine(maps);
		SeedResources(engine.World, credits: 5_000, scrap: 5_000, cores: 5);

		Assert.False(engine.CreateSimulation().TryEnqueue(CreateSyntheticPurchase(
			unitId,
			engine.World,
			EMerchantCatalog.Weapons,
			MerchantPurchaseTestHarness.ScrapDroneSwarmPortDamageUpgrade,
			MerchantPurchaseTestHarness.ScrapDroneSwarmPortTarget,
			ship.Clone())));
	}

	[Fact]
	public void TryEnqueue_DamagePack_RejectsTargetNotListedByQuote()
	{
		var (engine, unitId, ship, _) = MerchantPurchaseTestHarness.CreateEngine(maps);
		MerchantCatalogTestBands.SetBand(engine.World, EMerchantAvailabilityBand.T1);
		SeedResources(engine.World, credits: 5_000, scrap: 5_000, cores: 5);
		var invalidTarget = new MerchantCatalog.Target(
			new AbilityMount(EAbilityKind.GoopGun, ESpatialOrientation.Forward));

		Assert.False(engine.CreateSimulation().TryEnqueue(CreateSyntheticPurchase(
			unitId,
			engine.World,
			EMerchantCatalog.Weapons,
			MerchantPurchaseTestHarness.ScrapDroneSwarmPortDamageUpgrade,
			invalidTarget,
			ship.Clone())));
	}

	[Fact]
	public void Commit_ShieldPack_UpgradesOnlySelectedFace()
	{
		var (engine, unitId, ship, _) = MerchantPurchaseTestHarness.CreateEngine(maps);
		SeedResources(engine.World, credits: 5_000, scrap: 5_000);
		var face = ESpatialOrientation.Dorsal;
		var target = new MerchantCatalog.Target(Face: face);
		var offering = new MerchantCatalog.Offering(
			MerchantCatalog.Kind.UpgradeMaxShields,
			1);
		var before = ship.Clone();

		var records = engine.Commit(CreateAction(
			unitId,
			engine.World,
			EMerchantCatalog.ShipSupport,
			offering,
			target,
			before,
			engine));

		var after = Assert.Single(records.OfType<Record<MerchantShipPurchase>>()).Value.After;
		Assert.Equal(1, after.Loadout.ShieldUpgradeTiers[face]);
		Assert.Equal(
			before.Loadout.MaxShieldPoints[face] + 100,
			after.Loadout.MaxShieldPoints[face]);
		Assert.Equal(
			before.Loadout.MaxShieldPoints[ESpatialOrientation.Forward],
			after.Loadout.MaxShieldPoints[ESpatialOrientation.Forward]);
	}

	[Fact]
	public void Commit_BuyFuel_DebitsCreditsAndGrantsFuel()
	{
		var (engine, unitId, ship, _) = MerchantPurchaseTestHarness.CreateEngine(maps);
		MerchantCatalogTestBands.SetBand(engine.World, EMerchantAvailabilityBand.T0);
		SeedResources(engine.World, credits: 500, scrap: 0);
		var initialCredits = engine.World.PlayerResources.GetBalance(ResourceId.Credits);
		var initialFuel = engine.World.PlayerResources.GetBalance(ResourceId.Fuel);

		var records = engine.Commit(CreateAction(
			unitId,
			engine.World,
			EMerchantCatalog.Healing,
			MerchantPurchaseTestHarness.BuyFuel,
			default,
			ship.Clone(),
			engine));

		Assert.DoesNotContain(records, entry => entry is Record<MerchantShipPurchase>);
		var transaction = Assert.Single(records.OfType<Record<Transaction>>()).Value;
		Assert.Equal(TransactionSource.FuelPurchase, transaction.Source);
		Assert.Equal(
			initialCredits - StrategicEconomyConfig.FuelPurchaseCreditCost,
			engine.World.PlayerResources.GetBalance(ResourceId.Credits));
		Assert.Equal(
			initialFuel + StrategicEconomyConfig.FuelPurchaseAmount,
			engine.World.PlayerResources.GetBalance(ResourceId.Fuel));
	}

	[Fact]
	public void Commit_Repair_RemainsAvailableAtT2()
	{
		var (engine, unitId, ship, _) = MerchantPurchaseTestHarness.CreateEngine(maps);
		MerchantCatalogTestBands.SetBand(engine.World, EMerchantAvailabilityBand.T2);
		ship.HullPoints = 1;
		SeedResources(engine.World, credits: 500, scrap: 500);

		var records = engine.Commit(CreateAction(
			unitId,
			engine.World,
			EMerchantCatalog.Healing,
			MerchantPurchaseTestHarness.RepairHull,
			default,
			ship.Clone(),
			engine));

		var after = Assert.Single(records.OfType<Record<MerchantShipPurchase>>()).Value.After;
		Assert.Equal(after.Loadout.MaxHullPoints, after.HullPoints);
	}

	[Fact]
	public void Commit_RechargeAll_RemainsAvailableAtT2()
	{
		var (engine, unitId, ship, _) = MerchantPurchaseTestHarness.CreateEngine(maps);
		MerchantCatalogTestBands.SetBand(engine.World, EMerchantAvailabilityBand.T2);
		ship.ShieldPoints.Fill(0);
		SeedResources(engine.World, credits: 5_000, scrap: 0);

		var records = engine.Commit(CreateAction(
			unitId,
			engine.World,
			EMerchantCatalog.Healing,
			MerchantPurchaseTestHarness.RechargeAllShields,
			default,
			ship.Clone(),
			engine));

		var after = Assert.Single(records.OfType<Record<MerchantShipPurchase>>()).Value.After;
		Assert.Equal(after.TotalMaxShieldPoints, after.TotalCurrentShieldPoints);
	}

	[Fact]
	public void TryEnqueue_RejectsStaleShipSnapshot()
	{
		var (engine, unitId, ship, registry) = MerchantPurchaseTestHarness.CreateEngine(maps);
		MerchantCatalogTestBands.SetBand(engine.World, EMerchantAvailabilityBand.T0_5);
		SeedResources(engine.World, credits: 5_000, scrap: 5_000, cores: 5);
		var before = ship.Clone();
		Assert.True(ship.TryWithDamageUpgradeTier(
			MerchantPurchaseTestHarness.LightningCannonForwardTarget.Mount!.Value,
			1,
			out ship));
		registry.Update(ship);

		var action = CreateAction(
			unitId,
			engine.World,
			EMerchantCatalog.Weapons,
			MerchantPurchaseTestHarness.ScrapDroneSwarmPortDamageUpgrade,
			MerchantPurchaseTestHarness.ScrapDroneSwarmPortTarget,
			before,
			engine);
		Assert.False(engine.CreateSimulation().TryEnqueue(action));
	}

	[Fact]
	public void TryEnqueue_RejectsOperatorServingDifferentCatalog()
	{
		var (engine, unitId, ship, _) = MerchantPurchaseTestHarness.CreateEngine(maps);
		MerchantCatalogTestBands.SetBand(engine.World, EMerchantAvailabilityBand.T0_5);
		SeedResources(engine.World, credits: 5_000, scrap: 5_000, cores: 5);
		var action = CreateAction(
			unitId,
			engine.World,
			EMerchantCatalog.Weapons,
			MerchantPurchaseTestHarness.ScrapDroneSwarmPortDamageUpgrade,
			MerchantPurchaseTestHarness.ScrapDroneSwarmPortTarget,
			ship.Clone(),
			engine);
		action = action with
		{
			Endpoint = new FacilityMerchantEndpoint(
				SupplySystemPlan.Copper.TradeHubPoiId,
				DockyardFacilityId,
				MapFacilityOperators.ShieldOperatorName(engine.World)),
		};

		Assert.False(engine.CreateSimulation().TryEnqueue(action));
	}

	[Fact]
	public void TryEnqueue_RejectsInsufficientResources()
	{
		var (engine, unitId, ship, _) = MerchantPurchaseTestHarness.CreateEngine(maps);
		MerchantCatalogTestBands.SetBand(engine.World, EMerchantAvailabilityBand.T1);

		Assert.False(engine.CreateSimulation().TryEnqueue(CreateSyntheticPurchase(
			unitId,
			engine.World,
			EMerchantCatalog.Weapons,
			MerchantPurchaseTestHarness.ScrapDroneSwarmPortDamageUpgrade,
			MerchantPurchaseTestHarness.ScrapDroneSwarmPortTarget,
			ship.Clone())));
	}

	private static PurchaseAction CreateSyntheticPurchase(
		string unitId,
		StarMap map,
		EMerchantCatalog catalog,
		MerchantCatalog.Offering offering,
		MerchantCatalog.Target target,
		ShipInstance before) =>
		new(
			unitId,
			CreateEndpoint(map, catalog),
			catalog,
			offering,
			target,
			before,
			ResourceBundle.Of(ResourceId.Credits, 1));

	private static FacilityMerchantEndpoint CreateEndpoint(StarMap map, EMerchantCatalog catalog) =>
		new(
			SupplySystemPlan.Copper.TradeHubPoiId,
			DockyardFacilityId,
			catalog switch
			{
				EMerchantCatalog.Weapons => MapFacilityOperators.ShopOperatorName(map),
				EMerchantCatalog.ShipSupport => MapFacilityOperators.ShieldOperatorName(map),
				EMerchantCatalog.Healing => FacilityOperator.HealingStationName,
				_ => throw new ArgumentOutOfRangeException(nameof(catalog), catalog, null),
			});

	private static PurchaseAction CreateAction(
		string unitId,
		StarMap map,
		EMerchantCatalog catalog,
		MerchantCatalog.Offering offering,
		MerchantCatalog.Target target,
		ShipInstance before,
		Engine<StarMap, ActorRuntime> engine) =>
		MerchantPurchaseTestHarness.CreateQuotedPurchase(
			engine,
			unitId,
			CreateEndpoint(map, catalog),
			catalog,
			offering,
			target,
			before);

	private static void SeedResources(StarMap map, int credits, int scrap, int cores = 0)
	{
		var runtime = new ActorRuntime();
		new ChangeResourceEffect(
				TransactionSource.MerchantPurchase,
				ResourceBundle.Create(
					(ResourceId.Credits, credits),
					(ResourceId.ScrapAlloy, scrap),
					(ResourceId.IndustrialCore, cores)))
			.Apply(map, runtime, "seed");
	}

	private static int Amount(ResourceBundle bundle, ResourceId resource)
	{
		Assert.True(bundle.TryGet(resource, out var amount));
		return amount;
	}
}
