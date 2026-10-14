using GrimSpace.Core.Engine;
using GrimSpace.Run;
using GrimSpace.Run.Persistence;
using GrimSpace.Units;
using GrimSpace.Units.Specs;
using GrimSpace.World.StarSystem;
using GrimSpace.World.StarSystem.Actions;
using GrimSpace.World.StarSystem.Contracts;
using GrimSpace.World.StarSystem.Contracts.Objectives;
using GrimSpace.World.StarSystem.Effects;
using GrimSpace.World.StarSystem.Merchants;
using GrimSpace.World.StarSystem.Poi;
using GrimSpace.World.StarSystem.Presentation.Facilities;
using GrimSpace.World.StarSystem.Resources;
using GrimSpace.World.StarSystem.Runtime;
using GrimSpace.Tests.World.StarSystem.Contracts;
using GrimSpace.Tests.World.StarSystem.Merchants;
using GrimSpace.Tests.World.StarSystem.Traffic;

namespace GrimSpace.Tests.World.StarSystem.Contracts;

[StarSystemTestSuite]
public sealed class LeadSaleSavePersistenceTests
{
	[Fact]
	public void AcceptSaveLoad_KeepsSalePresentationAndPurchases()
	{
		var registry = PersistenceRegistry.CreateDefault();
		var runRegistry = new RunShipRegistry();
		var map = StarMap.Create(42, runRegistry);
		MerchantCatalogTestBands.SetBand(map, EMerchantAvailabilityBand.T1);
		StarSystemTestHarness.AddPlayerFleet(map, State.PlayerFleetUnitId);
		var contract = ContractFactory.Build(
			map,
			"lead-save-roundtrip",
			EContractKind.Lead,
			new LeadCreateArgs(
				ELeadKind.BlackMarketSale,
				new ContractNarrative("Quiet Opportunity", "A seller is available."),
				300,
				SpaceMeetingChance: 0,
				MerchantSubtype: EMerchantSubtype.Weapons));
		Assert.True(map.ContractRegistry.TryAdd(contract));
		var shipId = map.FleetRegistry.FleetOf(State.PlayerFleetUnitId).Members[0].Id;
		runRegistry.Register(ShipInstance.FromSpec(
			shipId,
			FighterSpec.Instance,
			ShipCatalog.FullFighterLoadout()));

		var runtimes = new ActorRuntimes<ActorRuntime>();
		runtimes.For(State.PlayerFleetUnitId);
		var engine = new Engine<StarMap, ActorRuntime>(map, runtimes);
		engine.Commit(ContractActionTestContext.Accept(engine.World, State.PlayerFleetUnitId, contract.Id));

		var captured = SaveDtoMapper.CaptureStarMap(engine.World, registry);
		var restored = SaveDtoMapper.RestoreStarMap(captured, registry, runRegistry);
		var lead = GetLeadState(restored, contract.Id);
		var sale = (BlackMarketSaleObjective)contract.Objective;
		var destination = (BlackMarketFacilityDestination)sale.Destination;

		Assert.True(restored.ContractRegistry.TryGet(contract.Id, out _));
		Assert.Equal(lead.DestinationOperatorName, lead.DestinationOperatorName);
		Assert.True(restored.GetPointOfInterest(destination.PoiId)
			.OperatorTemporaryRoles.TryGetAssignment(
				destination.FacilityId,
				lead.DestinationOperatorName!,
				out var assignment));
		Assert.Equal(EFacilityOperatorRole.Merchant, assignment.Role);
		Assert.Contains(contract.Id, assignment.SourceIds);

		MerchantCatalogTestBands.SetBand(restored, EMerchantAvailabilityBand.T1);
		SeedResources(restored, credits: 5_000, scrap: 5_000);
		var ship = runRegistry.Get(shipId);
		var (catalog, offering, target) = FirstPurchasableOffer(sale.MerchantSubtype, ship);
		var purchaseEngine = new Engine<StarMap, ActorRuntime>(
			restored,
			new ActorRuntimes<ActorRuntime>());
		Assert.True(purchaseEngine.CreateSimulation().TryEnqueue(
			MerchantPurchaseTestHarness.CreateQuotedPurchase(
				purchaseEngine,
				State.PlayerFleetUnitId,
				new LeadSaleMerchantEndpoint(contract.Id),
				catalog,
				offering,
				target,
				ship.Clone())));
	}

	private static LeadContractState GetLeadState(StarMap map, string contractId)
	{
		Assert.True(map.ContractRegistry.TryGetState(contractId, out var state));
		return Assert.IsType<LeadContractState>(state);
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

	private static (EMerchantCatalog Catalog, MerchantCatalog.Offering Offering, MerchantCatalog.Target Target)
		FirstPurchasableOffer(EMerchantSubtype subtype, ShipInstance ship)
	{
		var catalog = MerchantSubtypeRouting.CatalogFor(subtype);
		var offer = MerchantCatalog.ListFor(catalog, ship, EMerchantAvailabilityBand.T1).First();
		return (catalog, offer.Offering, offer.Targets[0]);
	}
}
