using GrimSpace.Core.Engine;
using GrimSpace.Run;
using GrimSpace.Run.Items;
using GrimSpace.Units;
using GrimSpace.Units.Specs;
using GrimSpace.World.StarSystem;
using GrimSpace.World.StarSystem.Actions;
using GrimSpace.World.StarSystem.Contracts;
using GrimSpace.World.StarSystem.Contracts.Objectives;
using GrimSpace.World.StarSystem.Effects;
using GrimSpace.World.StarSystem.Merchants;
using GrimSpace.World.StarSystem.Presentation.Facilities;
using GrimSpace.World.StarSystem.Resources;
using GrimSpace.World.StarSystem.Runtime;
using GrimSpace.Tests.World.StarSystem.Contracts;
using GrimSpace.Tests.World.StarSystem.Traffic;

namespace GrimSpace.Tests.World.StarSystem.Merchants;

[StarSystemTestSuite]
public sealed class LeadSalePurchaseActionTests
{
	[Fact]
	public void TryEnqueue_LeadSaleEndpoint_AcceptsFacilityMerchantPurchase()
	{
		var (engine, unitId, contract, ship) = CreateAcceptedLead("lead-purchase-facility", spaceMeetingChance: 0);
		var sale = (BlackMarketSaleObjective)contract.Objective;
		MerchantCatalogTestBands.SetBand(engine.World, EMerchantAvailabilityBand.T1);
		SeedResources(engine.World, credits: 5_000, scrap: 5_000);
		var before = ship.Clone();
		var (catalog, offering, target) = FirstPurchasableOffer(sale.MerchantSubtype, before);

		Assert.True(engine.CreateSimulation().TryEnqueue(
			MerchantPurchaseTestHarness.CreateQuotedPurchase(
				engine,
				unitId,
				new LeadSaleMerchantEndpoint(contract.Id),
				catalog,
				offering,
				target,
				before)));
	}

	[Fact]
	public void TryEnqueue_LeadSaleEndpoint_AcceptsSpaceMeetingPurchase()
	{
		var (engine, unitId, contract, ship) = CreateAcceptedLead("lead-purchase-space", spaceMeetingChance: 1);
		var sale = (BlackMarketSaleObjective)contract.Objective;
		MerchantCatalogTestBands.SetBand(engine.World, EMerchantAvailabilityBand.T1);
		SeedResources(engine.World, credits: 5_000, scrap: 5_000);
		var before = ship.Clone();
		var (catalog, offering, target) = FirstPurchasableOffer(sale.MerchantSubtype, before);

		Assert.True(engine.CreateSimulation().TryEnqueue(
			MerchantPurchaseTestHarness.CreateQuotedPurchase(
				engine,
				unitId,
				new LeadSaleMerchantEndpoint(contract.Id),
				catalog,
				offering,
				target,
				before)));
	}

	[Fact]
	public void TryEnqueue_LeadSaleEndpoint_RejectsAfterDeadline()
	{
		var (engine, unitId, contract, ship) = CreateAcceptedLead("lead-purchase-expired", spaceMeetingChance: 0);
		var lead = GetLeadState(engine.World, contract.Id);
		engine.World.Timeline.Clock.Set(lead.DeadlineTick + 1);
		var sale = (BlackMarketSaleObjective)contract.Objective;
		var (catalog, offering, target) = FirstPurchasableOffer(sale.MerchantSubtype, ship);

		Assert.False(engine.CreateSimulation().TryEnqueue(
			MerchantPurchaseTestHarness.CreateQuotedPurchase(
				engine,
				unitId,
				new LeadSaleMerchantEndpoint(contract.Id),
				catalog,
				offering,
				target,
				ship.Clone())));
	}

	private static (EMerchantCatalog Catalog, MerchantCatalog.Offering Offering, MerchantCatalog.Target Target)
		FirstPurchasableOffer(EMerchantSubtype subtype, ShipInstance ship)
	{
		if (subtype == EMerchantSubtype.Ships)
			throw new InvalidOperationException("Ship recruitment uses RecruitShipAction.");

		var catalog = MerchantSubtypeRouting.CatalogFor(subtype);
		var offer = MerchantCatalog.ListFor(catalog, ship, EMerchantAvailabilityBand.T1).First();
		return (catalog, offer.Offering, offer.Targets[0]);
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

	private static (Engine<StarMap, ActorRuntime> Engine, string UnitId, Contract Contract, ShipInstance Ship)
		CreateAcceptedLead(string contractId, double spaceMeetingChance)
	{
		var registry = new Inventory();
		var map = StarMap.Create(42, registry);
		MerchantCatalogTestBands.SetBand(map, EMerchantAvailabilityBand.T1);
		StarSystemTestHarness.AddPlayerFleet(map, State.PlayerFleetUnitId);
		var contract = ContractFactory.Build(
			map,
			contractId,
			EContractKind.Lead,
			new LeadCreateArgs(
				ELeadKind.BlackMarketSale,
				new ContractNarrative("Quiet Opportunity", "A seller is available."),
				300,
				SpaceMeetingChance: spaceMeetingChance,
				MerchantSubtype: EMerchantSubtype.Weapons));
		Assert.True(map.ContractRegistry.TryAdd(contract));
		var unitId = State.PlayerFleetUnitId;
		var shipId = map.FleetRegistry.FleetOf(unitId).Members[0].Id;
		var ship = ShipInstance.FromSpec(
			shipId,
			FighterSpec.Instance,
			ShipCatalog.FullFighterLoadout());
		registry.Register(ship, State.PlayerFleetUnitId);
		map = map.Fork();

		var runtimes = new ActorRuntimes<ActorRuntime>();
		runtimes.For(unitId);
		var engine = new Engine<StarMap, ActorRuntime>(map, runtimes);
		engine.Commit(ContractActionTestContext.Accept(engine.World, unitId, contract.Id));
		return (engine, unitId, contract, ship);
	}
}
