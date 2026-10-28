using GrimSpace.Core.Engine;
using GrimSpace.Core.Ids;
using GrimSpace.Run;
using GrimSpace.Run.Items;
using GrimSpace.Units;
using GrimSpace.Units.Enums;
using GrimSpace.World.StarSystem;
using GrimSpace.World.StarSystem.Actions;
using GrimSpace.World.StarSystem.Generation;
using GrimSpace.World.StarSystem.Effects;
using GrimSpace.World.StarSystem.Merchants;
using GrimSpace.World.StarSystem.Poi;
using GrimSpace.World.StarSystem.Poi.Concrete;
using GrimSpace.World.StarSystem.Resources;
using GrimSpace.World.StarSystem.Runtime;
using GrimSpace.Tests.World.StarSystem.Poi;
using GrimSpace.Tests.World.StarSystem.Traffic;
using BattleUnitType = GrimSpace.Units.Enums.EType;

namespace GrimSpace.Tests.World.StarSystem.Merchants;

[StarSystemTestSuite]
public sealed class PurchaseShipActionTests
{
	[Fact]
	public void TryEnqueue_DoesNotMutateStrategicFleetMembers()
	{
		var registry = new Inventory();
		var map = StarMap.Create(42, registry);
		MerchantCatalogTestBands.SetBand(map, EMerchantAvailabilityBand.T1);
		StarSystemTestHarness.AddPlayerFleet(map, State.PlayerFleetUnitId);
		var shipId = map.FleetRegistry.FleetOf(State.PlayerFleetUnitId).Members[0].Id;
		registry.Register(
			ShipInstance.FromCatalog(shipId, BattleUnitType.Fighter),
			State.PlayerFleetUnitId);
		SeedResources(map, credits: 10_000, scrap: 10_000);

		var offer = map.Market.ListRecruitmentOffers(EMerchantAvailabilityBand.T1).First();
		var purchasedId = TypedIdGenerator.NextId(UnitTypeSlug.For(offer.Chassis));
		var endpoint = new FacilityMerchantEndpoint(
			SupplySystemPlan.Copper.TradeHubPoiId,
			Facility.ScopedId(SupplySystemPlan.Copper.TradeHubPoiId, TradeHub.MarketFacilitySlug),
			MapFacilityOperators.ShipMerchantOperatorName(map));
		var action = new PurchaseShipAction(
			State.PlayerFleetUnitId,
			endpoint,
			new ShipSpawnDeclaration(purchasedId, offer.Chassis, offer.GearTier));
		var runtime = new ActorRuntime();
		Assert.True(PurchaseShipActionDef.Instance.IsLegal(action, map, runtime));

		var engine = new Engine<StarMap, ActorRuntime>(map, new ActorRuntimes<ActorRuntime>());
		Assert.True(engine.CreateSimulation().TryEnqueue(action));

		var membersAfter = map.FleetRegistry.FleetOf(State.PlayerFleetUnitId).Members;
		Assert.Single(membersAfter);
		Assert.Equal(shipId, membersAfter[0].Id);
		Assert.False(registry.TryGet(purchasedId, out _));
	}

	[Fact]
	public void CommitSetup_WithRun_RegistersOwnedShipWithoutPilotWhenActiveRosterFull()
	{
		using var run = State.CreateNewRun(42);
		MerchantCatalogTestBands.SetBand(run.StarSystem.Map, EMerchantAvailabilityBand.T1);
		for (var index = 0; index < RunPilotRegistry.MaxActivePilots - 1; index++)
		{
			var shipId = TypedIdGenerator.NextId("fighter");
			Assert.True(run.TryEnlistPlayerShip(new ShipSpawnDeclaration(shipId, BattleUnitType.Fighter)));
		}

		Assert.Equal(RunPilotRegistry.MaxActivePilots, run.PilotRegistry.Active.Count);
		var pilotCountBefore = run.PilotRegistry.All.Count;
		var ownedBefore = run.ItemInventory.ShipsOwnedBy(State.PlayerFleetUnitId).Count();

		var offer = run.StarSystem.Map.Market.ListRecruitmentOffers(EMerchantAvailabilityBand.T1).First();
		var purchasedId = TypedIdGenerator.NextId(UnitTypeSlug.For(offer.Chassis));
		SeedResources(run.StarSystem.Map, credits: 10_000, scrap: 10_000);
		var action = new PurchaseShipAction(
			State.PlayerFleetUnitId,
			new FacilityMerchantEndpoint(
				SupplySystemPlan.Copper.TradeHubPoiId,
				Facility.ScopedId(SupplySystemPlan.Copper.TradeHubPoiId, TradeHub.MarketFacilitySlug),
				MapFacilityOperators.ShipMerchantOperatorName(run.StarSystem.Map)),
			new ShipSpawnDeclaration(purchasedId, offer.Chassis, offer.GearTier));
		var runtime = run.StarSystem.RuntimeFor(State.PlayerFleetUnitId);
		Assert.True(PurchaseShipActionDef.Instance.IsLegal(action, run.StarSystem.Map, runtime));

		run.StarSystem.CommitSetup(action);

		Assert.Equal(pilotCountBefore, run.PilotRegistry.All.Count);
		Assert.Equal(ownedBefore + 1, run.ItemInventory.ShipsOwnedBy(State.PlayerFleetUnitId).Count());
		Assert.True(run.ItemInventory.TryGet(purchasedId, out _));
		Assert.False(run.PilotRegistry.TryGetForShip(purchasedId, out _));
		Assert.DoesNotContain(purchasedId, run.GetFieldedShipIds());
	}

	private static void SeedResources(StarMap map, int credits, int scrap)
	{
		var runtime = new ActorRuntime();
		new ChangeResourceEffect(
				TransactionSource.MerchantPurchase,
				ResourceBundle.Create(
					(ResourceId.Credits, credits),
					(ResourceId.ScrapAlloy, scrap)))
			.Apply(map, runtime, "seed");
	}
}
