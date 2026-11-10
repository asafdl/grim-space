using GrimSpace.Tests.Run;
using GrimSpace.Core.Engine;
using GrimSpace.Core.Ids;
using GrimSpace.Run;
using GrimSpace.Run.Items;
using GrimSpace.Run.Persistence;
using GrimSpace.Pilots;
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
	[Theory]
	[InlineData(EShipGearTier.T0)]
	[InlineData(EShipGearTier.T1)]
	[InlineData(EShipGearTier.T2)]
	public void PurchaseRogue_AssignsHiredRoguePilotAndPersists(EShipGearTier tier)
	{
		using var run = State.CreateNewRun(42);
		var map = run.StarSystem.Map;
		Assert.True(map.Market.TryFindRecruitmentOffer(EType.Rogue, tier, out var offer));
		SeedResources(map, credits: 10_000, scrap: 10_000);
		new ChangeResourceEffect(TransactionSource.MerchantPurchase,
			ResourceBundle.Of(ResourceId.IndustrialCore, 10))
			.Apply(map, new ActorRuntime(), "seed");
		var id = TypedIdGenerator.NextId("rogue");
		var action = new PurchaseShipAction(
			State.PlayerFleetUnitId,
			new FacilityMerchantEndpoint(
				SupplySystemPlan.Copper.TradeHubPoiId,
				Facility.ScopedId(SupplySystemPlan.Copper.TradeHubPoiId, TradeHub.MarketFacilitySlug),
				MapFacilityOperators.ShipMerchantOperatorName(map)),
			new ShipSpawnDeclaration(id, offer.Chassis, offer.GearTier));
		Assert.True(PurchaseShipActionDef.Instance.IsLegal(action, map,
			run.StarSystem.RuntimeFor(State.PlayerFleetUnitId)));

		run.StarSystem.CommitSetup(action);

		Assert.True(run.ItemInventory.TryGet(id, out var ship));
		Assert.Equal(EType.Rogue, ship.Spec.Chassis);
		Assert.Equal(tier, ShipLoadoutGearTier.FromLoadout(ship.Loadout, ship.Spec.Chassis));
		Assert.False(run.PilotRegistry.TryGetForShip(id, out _));
		var profile = new PilotRecruitProfile("Rogue Hire", "pilot_03",
			EPilotBackgroundId.PodRacer, classId: EOperatorClassId.Rogue);
		Assert.True(run.TryAcquirePilotHire(new PilotHireCommitted(profile)));
		var pilot = Assert.Single(run.PilotRegistry.All, candidate => candidate.Name == profile.Name);
		Assert.False(run.TryAssignPilotToShip(pilot.Id, run.GetFieldedShipIds()[0]));
		Assert.True(run.TryAssignPilotToShip(pilot.Id, id));

		var registry = PersistenceRegistry.CreateDefault();
		using var restored = State.FromSnapshot(run.CaptureSnapshot(registry), registry);
		Assert.True(restored.ItemInventory.TryGet(id, out var restoredShip));
		Assert.Equal(EType.Rogue, restoredShip.Spec.Chassis);
		Assert.Equal(tier, ShipLoadoutGearTier.FromLoadout(restoredShip.Loadout, restoredShip.Spec.Chassis));
		Assert.True(restored.PilotRegistry.TryGetForShip(id, out var restoredPilot));
		Assert.Equal(EOperatorClassId.Rogue, restoredPilot.ClassId);
		Assert.True(restored.StarSystem.Map.Market.TryFindRecruitmentOffer(EType.Rogue, tier, out var restoredOffer));
		Assert.Equal(offer.Cost, restoredOffer.Cost);
	}

	[Fact]
	public void TryEnqueue_DoesNotMutateStrategicFleetMembers()
	{
		var registry = new Inventory();
		var map = StarMap.Create(42, registry);
		StarSystemTestHarness.AddPlayerFleet(map, State.PlayerFleetUnitId);
		var shipId = map.FleetRegistry.FleetOf(State.PlayerFleetUnitId).Members[0].Id;
		registry.Register(
			ShipInstance.FromCatalog(shipId, BattleUnitType.Fighter),
			State.PlayerFleetUnitId);
		SeedResources(map, credits: 10_000, scrap: 10_000);

		var offer = map.Market.ListRecruitmentOffers()
			.First(candidate => candidate.GearTier == EShipGearTier.T1);
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
	public void CommitSetup_WithRun_RegistersOwnedShipWithoutPilotWithFourAssignedPilots()
	{
		using var run = State.CreateNewRun(42);
		RunTestKit.EnlistFourPilots(run);

		Assert.Equal(4, run.PilotRegistry.Assigned.Count);
		var pilotCountBefore = run.PilotRegistry.All.Count;
		var ownedBefore = run.ItemInventory.ShipsOwnedBy(State.PlayerFleetUnitId).Count();

		var offer = run.StarSystem.Map.Market.ListRecruitmentOffers()
			.First(candidate => candidate.GearTier == EShipGearTier.T1);
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
