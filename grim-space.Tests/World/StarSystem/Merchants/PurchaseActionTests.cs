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
			FacilityOperator.HealingStationName);

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
