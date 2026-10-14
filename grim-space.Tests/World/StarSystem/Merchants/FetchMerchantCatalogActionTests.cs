using GrimSpace.Core.Actions;
using GrimSpace.Core.Engine;
using GrimSpace.Math.Grid;
using GrimSpace.Pilots;
using GrimSpace.Run;
using GrimSpace.Units;
using GrimSpace.World.Factions;
using GrimSpace.World.StarSystem;
using GrimSpace.World.StarSystem.Actions;
using GrimSpace.World.StarSystem.Contact;
using GrimSpace.World.StarSystem.Effects;
using GrimSpace.World.StarSystem.Generation;
using GrimSpace.World.StarSystem.Merchants;
using GrimSpace.World.StarSystem.Poi;
using GrimSpace.World.StarSystem.Resources;
using GrimSpace.World.StarSystem.Runtime;
using GrimSpace.World.StarSystem.Skills;
using GrimSpace.World.StarSystem.Units;
using GrimSpace.Tests.World.StarSystem.Poi;
using GrimSpace.Tests.World.StarSystem.Traffic;

namespace GrimSpace.Tests.World.StarSystem.Merchants;

[StarSystemTestSuite]
public sealed class FetchMerchantCatalogActionTests(StarMapFixture maps)
{
	private const string DockyardFacilityId = "poi-trade-dockyard";

	[Fact]
	public void Commit_PublishesQuoteWithBaseMarketOffers()
	{
		var (engine, unitId, ship, _) = MerchantPurchaseTestHarness.CreateEngine(maps);
		MerchantCatalogTestBands.SetBand(engine.World, EMerchantAvailabilityBand.T0_5);
		var before = ship.Clone();
		var endpoint = CreateEndpoint(engine.World, EMerchantCatalog.Weapons);

		var records = engine.Commit(new FetchMerchantCatalogAction(
			unitId,
			endpoint,
			EMerchantCatalog.Weapons,
			before));

		var quote = Assert.Single(records.OfType<Record<MerchantCatalogQuote>>()).Value;
		Assert.Equal(before.Id, quote.ShipId);
		Assert.Equal(endpoint, quote.Endpoint);
		Assert.Equal(EMerchantCatalog.Weapons, quote.Catalog);

		var baseOffers = MerchantCatalog.ListFor(
			EMerchantCatalog.Weapons,
			before,
			engine.World);
		Assert.Equal(baseOffers.Count, quote.Offers.Count);
		Assert.Equal(
			baseOffers[0].Cost,
			quote.Offers[0].Cost);
	}

	[Fact]
	public void Commit_AppliesIKnowABotDiscountThroughPipeline()
	{
		var pilots = new RunPilotRegistry();
		for (var index = 0; index < 2; index++)
		{
			var pilot = PilotProgression
				.AddExperience(Pilot.CreateForShip($"fighter-{index}"), 450)
				.LearnPilotSkill(EPilotSkillId.IKnowABot);
			pilots.Register(pilot);
		}

		var grants = pilots.QueryLearned(RunPilotRegistry.IsStrategicPassiveTag);

		var (engine, unitId, ship, _) = MerchantPurchaseTestHarness.CreateEngine(
			maps,
			strategicGrants: grants);
		MerchantCatalogTestBands.SetBand(engine.World, EMerchantAvailabilityBand.T0_5);
		var before = ship.Clone();
		var offering = MerchantPurchaseTestHarness.ScrapDroneSwarmPortDamageUpgrade;
		var target = MerchantPurchaseTestHarness.ScrapDroneSwarmPortTarget;

		var records = engine.Commit(new FetchMerchantCatalogAction(
			unitId,
			CreateEndpoint(engine.World, EMerchantCatalog.Weapons),
			EMerchantCatalog.Weapons,
			before));

		var quote = Assert.Single(records.OfType<Record<MerchantCatalogQuote>>()).Value;
		var baseOffer = MerchantCatalog.ListFor(
				EMerchantCatalog.Weapons,
				before,
				engine.World)
			.First(offer => offer.Offering == offering && offer.Targets.Contains(target));
		var discounted = quote.Offers.First(offer =>
			offer.Offering == offering && offer.Targets.Contains(target));

		Assert.Equal(96, Amount(discounted.Cost, ResourceId.Credits));
		Assert.Equal(224, Amount(discounted.Cost, ResourceId.ScrapAlloy));
		Assert.NotEqual(baseOffer.Cost, discounted.Cost);
	}

	[Fact]
	public void Commit_AfterStrategicRefresh_UsesRebuiltPipeline()
	{
		var (engine, unitId, ship, _) = MerchantPurchaseTestHarness.CreateEngine(maps);
		MerchantCatalogTestBands.SetBand(engine.World, EMerchantAvailabilityBand.T0_5);
		var endpoint = CreateEndpoint(engine.World, EMerchantCatalog.Weapons);
		var before = ship.Clone();

		engine.Commit(new RefreshStrategicPilotSkillsAction(
			unitId,
			[new PilotSkillGrant(EPilotSkillId.IKnowABot, "pilot-1")]));
		var records = engine.Commit(new FetchMerchantCatalogAction(
			unitId,
			endpoint,
			EMerchantCatalog.Weapons,
			before));

		var quote = Assert.Single(records.OfType<Record<MerchantCatalogQuote>>()).Value;
		var discounted = quote.Offers.First(offer =>
			offer.Offering == MerchantPurchaseTestHarness.ScrapDroneSwarmPortDamageUpgrade
			&& offer.Targets.Contains(MerchantPurchaseTestHarness.ScrapDroneSwarmPortTarget));
		Assert.Equal(108, Amount(discounted.Cost, ResourceId.Credits));
		Assert.Equal(252, Amount(discounted.Cost, ResourceId.ScrapAlloy));
	}

	[Fact]
	public void ImmediateFetch_WhileWaitingForPlayerInput_CommitsQuoteWithoutAdvancingTick()
	{
		var (engine, unitId, ship, _) = MerchantPurchaseTestHarness.CreateEngine(maps);
		MerchantCatalogTestBands.SetBand(engine.World, EMerchantAvailabilityBand.T0_5);
		var pirateId = AddPirate(engine.World, new Coord(4, 0, 0));
		new SetEngagementIntentEffect(unitId, pirateId)
			.Apply(engine.World, new ActorRuntime(), unitId);
		new SetTravelTargetEffect(unitId, TravelTarget.Fleet(pirateId, EContactIntent.Engagement))
			.Apply(engine.World, new ActorRuntime(), unitId);
		engine.Commit([new ReachContactAction(unitId, pirateId)]);
		Assert.True(engine.World.WaitingForPlayerInput);

		using var orchestrator = StarSystemTestHarness.CreatePlayerOrchestrator(
			maps,
			unitId,
			42,
			map: engine.World);
		var sink = Assert.IsAssignableFrom<IImmediateActionSink>(orchestrator.ImmediatePlayerActions);
		MerchantCatalogQuote? quote = null;
		using var subscription = orchestrator.Subscribe<Record<MerchantCatalogQuote>>(
			record => quote = record.Value);
		var tick = orchestrator.Tick;
		var endpoint = CreateEndpoint(orchestrator.Map, EMerchantCatalog.Weapons);

		Assert.True(sink.TryEnqueue([
			new FetchMerchantCatalogAction(
				unitId,
				endpoint,
				EMerchantCatalog.Weapons,
				ship.Clone()),
		]));
		Assert.True(sink.CommitImmediately());
		Assert.Equal(tick, orchestrator.Tick);
		Assert.NotNull(quote);
		Assert.Equal(endpoint, quote.Endpoint);
		Assert.True(orchestrator.PlayerAgent!.IsPlanning);
	}

	[Fact]
	public void TryEnqueue_Purchase_RejectsStaleQuotedCost()
	{
		var (engine, unitId, ship, _) = MerchantPurchaseTestHarness.CreateEngine(maps);
		MerchantCatalogTestBands.SetBand(engine.World, EMerchantAvailabilityBand.T0_5);
		SeedResources(engine.World, credits: 5_000, scrap: 5_000, cores: 5);
		var before = ship.Clone();
		var action = MerchantPurchaseTestHarness.CreateQuotedPurchase(
			engine,
			unitId,
			CreateEndpoint(engine.World, EMerchantCatalog.Weapons),
			EMerchantCatalog.Weapons,
			MerchantPurchaseTestHarness.ScrapDroneSwarmPortDamageUpgrade,
			MerchantPurchaseTestHarness.ScrapDroneSwarmPortTarget,
			before);
		var stale = action with
		{
			QuotedCost = ResourceBundle.Of(ResourceId.Credits, 1),
		};

		Assert.False(engine.CreateSimulation().TryEnqueue(stale));
	}

	private static string AddPirate(StarMap map, Coord coord, string id = "pirate-a")
	{
		map.FleetRegistry.Add(StarSystemTestHarness.CreatePirateFleet(
			id,
			coord,
			EFaction.Pirates));
		return id;
	}

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
