using GrimSpace.Core.Ids;
using GrimSpace.Run;
using GrimSpace.Run.Persistence;
using GrimSpace.Units;
using GrimSpace.Units.Enums;
using GrimSpace.World.StarSystem;
using GrimSpace.World.StarSystem.Actions;
using GrimSpace.World.StarSystem.Effects;
using GrimSpace.World.StarSystem.Generation;
using GrimSpace.World.StarSystem.Merchants;
using GrimSpace.World.StarSystem.Poi;
using GrimSpace.World.StarSystem.Poi.Concrete;
using GrimSpace.World.StarSystem.Resources;
using GrimSpace.World.StarSystem.Runtime;
using GrimSpace.Tests.World.StarSystem.Merchants;
using GrimSpace.Tests.World.StarSystem.Poi;
using BattleUnitType = GrimSpace.Units.Enums.EType;
using RunState = GrimSpace.Run.State;

namespace GrimSpace.Tests.Run;

[IntegrationTestSuite]
public sealed class PlayerShipAcquireTests
{
	[Fact]
	public void TryAcquirePlayerShip_RejectsDuplicateInventoryEntryWithoutRegisteringPilot()
	{
		using var run = RunState.CreateNewRun(42);
		var declaration = new ShipSpawnDeclaration("duplicate-ship", BattleUnitType.Gunship);
		run.ItemInventory.Register(declaration, RunState.PlayerFleetUnitId);
		var pilotCountBefore = run.PilotRegistry.All.Count;

		Assert.False(run.TryAcquirePlayerShip(declaration));
		Assert.Equal(pilotCountBefore, run.PilotRegistry.All.Count);
		Assert.False(run.PilotRegistry.TryGetForShip(declaration.ShipId, out _));
	}

	[Fact]
	public void TryEnlistPlayerShip_SyncsWorldFleetMembersToFieldedShips()
	{
		using var run = RunState.CreateNewRun(42);
		var secondId = TypedIdGenerator.NextId("gunship");
		Assert.True(run.TryEnlistPlayerShip(new ShipSpawnDeclaration(secondId, BattleUnitType.Gunship)));

		var worldFleet = run.StarSystem.Map.FleetRegistry.FleetOf(RunState.PlayerFleetUnitId);
		Assert.Equal(run.GetFieldedShipIds(), worldFleet.Members.Select(member => member.Id).ToArray());
		Assert.Equal(
			run.GetFieldedShipIds(),
			worldFleet.Registrations.Select(declaration => declaration.ShipId).ToArray());
	}

	[Fact]
	public void CanAcquirePlayerShip_BlocksWhenActivePilotCapWouldBeExceededOnMap()
	{
		using var run = RunState.CreateNewRun(42);
		for (var index = 0; index < RunPilotRegistry.MaxActivePilots - 1; index++)
		{
			var shipId = TypedIdGenerator.NextId("gunship");
			Assert.True(run.TryEnlistPlayerShip(new ShipSpawnDeclaration(shipId, BattleUnitType.Gunship)));
		}

		var blockedId = TypedIdGenerator.NextId("gunship");
		Assert.False(run.CanAcquirePlayerShip(new ShipSpawnDeclaration(blockedId, BattleUnitType.Gunship)));
	}

	[Fact]
	public void TryAcquireOwnedShip_RegistersInventoryWithoutPilot()
	{
		using var run = RunState.CreateNewRun(42);
		var declaration = new ShipSpawnDeclaration("reserve-hull", BattleUnitType.Gunship);
		var pilotCountBefore = run.PilotRegistry.All.Count;

		Assert.True(run.TryAcquireOwnedShip(declaration));
		Assert.True(run.ItemInventory.TryGet(declaration.ShipId, out _));
		Assert.Equal(pilotCountBefore, run.PilotRegistry.All.Count);
		Assert.False(run.PilotRegistry.TryGetForShip(declaration.ShipId, out _));
	}

	[Fact]
	public void FromSnapshot_RoundTripsOwnedUnfieldedShip()
	{
		using var run = RunState.CreateNewRun(42);
		var declaration = new ShipSpawnDeclaration("reserve-hull", BattleUnitType.Gunship);
		Assert.True(run.TryAcquireOwnedShip(declaration));

		var registry = PersistenceRegistry.CreateDefault();
		using var restored = RunState.FromSnapshot(run.CaptureSnapshot(registry), registry);

		Assert.True(restored.ItemInventory.TryGet(declaration.ShipId, out _));
		Assert.False(restored.PilotRegistry.TryGetForShip(declaration.ShipId, out _));
		Assert.DoesNotContain(declaration.ShipId, restored.GetFieldedShipIds());
	}

	[Fact]
	public void PurchaseShipAction_RemainsLegalWhenActivePilotCapIsFull()
	{
		using var run = RunState.CreateNewRun(42);
		MerchantCatalogTestBands.SetBand(run.StarSystem.Map, EMerchantAvailabilityBand.T1);
		for (var index = 0; index < RunPilotRegistry.MaxActivePilots - 1; index++)
		{
			var shipId = TypedIdGenerator.NextId("gunship");
			Assert.True(run.TryEnlistPlayerShip(new ShipSpawnDeclaration(shipId, BattleUnitType.Gunship)));
		}

		var offer = run.StarSystem.Map.Market.ListRecruitmentOffers(EMerchantAvailabilityBand.T1).First();
		var purchasedId = TypedIdGenerator.NextId(UnitTypeSlug.For(offer.Chassis));
		SeedResources(run.StarSystem.Map, credits: 10_000, scrap: 10_000);
		var action = new PurchaseShipAction(
			RunState.PlayerFleetUnitId,
			new FacilityMerchantEndpoint(
				SupplySystemPlan.Copper.TradeHubPoiId,
				Facility.ScopedId(SupplySystemPlan.Copper.TradeHubPoiId, TradeHub.MarketFacilitySlug),
				MapFacilityOperators.ShipMerchantOperatorName(run.StarSystem.Map)),
			new ShipSpawnDeclaration(purchasedId, offer.Chassis, offer.GearTier));
		var runtime = run.StarSystem.RuntimeFor(RunState.PlayerFleetUnitId);

		Assert.True(PurchaseShipActionDef.Instance.IsLegal(action, run.StarSystem.Map, runtime));
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
