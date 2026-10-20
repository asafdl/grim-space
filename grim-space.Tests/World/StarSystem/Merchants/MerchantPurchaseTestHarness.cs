using GrimSpace.Core.Engine;
using GrimSpace.Pilots;
using GrimSpace.Run;
using GrimSpace.Run.Items;
using GrimSpace.World.StarSystem.Actions;
using GrimSpace.World.StarSystem.Poi;
using GrimSpace.Units;
using GrimSpace.Units.Enums;
using GrimSpace.Units.Loadouts.Abilities;
using GrimSpace.Units.Specs;
using GrimSpace.Math.Grid;
using GrimSpace.World.StarSystem;
using GrimSpace.World.StarSystem.Merchants;
using GrimSpace.World.StarSystem.Runtime;
using GrimSpace.World.StarSystem.PilotCapabilities;
using GrimSpace.Tests.World.StarSystem.Traffic;

namespace GrimSpace.Tests.World.StarSystem.Merchants;

internal static class MerchantPurchaseTestHarness
{
	public static MerchantCatalog.Offering ScrapDroneSwarmPortDamageUpgrade =>
		new(MerchantCatalog.Kind.UpgradeDamage, 1);

	public static MerchantCatalog.Offering LightningCannonForwardDamageUpgrade =>
		new(MerchantCatalog.Kind.UpgradeDamage, 1);

	public static MerchantCatalog.Offering ScrapDroneSwarmPortRangeUpgrade =>
		new(MerchantCatalog.Kind.UpgradeRange, 1);

	public static MerchantCatalog.Target ScrapDroneSwarmPortTarget =>
		new(new AbilityMount(EAbilityKind.ScrapDroneSwarm, ESpatialOrientation.Port));

	public static MerchantCatalog.Target LightningCannonForwardTarget =>
		new(new AbilityMount(EAbilityKind.LightningCannon, ESpatialOrientation.Forward));

	public static MerchantCatalog.Offering RepairHull => new(MerchantCatalog.Kind.RepairHull);

	public static MerchantCatalog.Offering RechargeAllShields =>
		new(MerchantCatalog.Kind.RechargeAllShields);

	public static (Engine<StarMap, ActorRuntime> engine, string unitId, ShipInstance ship, Inventory registry) CreateEngine(
		StarMapFixture maps,
		int seed = 42,
		IReadOnlyList<PilotCapabilityGrant>? strategicGrants = null)
	{
		var registry = new Inventory();
		var map = StarMap.Create(seed, registry);
		StarSystemTestHarness.AddPlayerFleet(map, State.PlayerFleetUnitId);
		var unitId = State.PlayerFleetUnitId;
		var shipId = map.FleetRegistry.FleetOf(unitId).Members[0].Id;
		var ship = ShipInstance.FromSpec(
			shipId,
			FighterSpec.Instance,
			ShipCatalog.FullFighterLoadout());
		registry.Register(ship, State.PlayerFleetUnitId);
		map = map.Fork();

		var runtimes = new ActorRuntimes<ActorRuntime>();
		var runtime = runtimes.For(unitId);
		if (strategicGrants is not null)
			runtime.ReplaceActionEffects(
				StarMapPilotCapabilityPipelineFactory.Build(strategicGrants));
		var engine = new Engine<StarMap, ActorRuntime>(map, runtimes);
		return (engine, unitId, ship, registry);
	}

	public static PurchaseAction CreateQuotedPurchase(
		Engine<StarMap, ActorRuntime> engine,
		string unitId,
		MerchantEndpoint endpoint,
		EMerchantCatalog catalog,
		MerchantCatalog.Offering offering,
		MerchantCatalog.Target target,
		ShipInstance before) =>
		CreateQuotedPurchase(
			engine.World,
			engine.ActorRuntimes.For(unitId),
			unitId,
			endpoint,
			catalog,
			offering,
			target,
			before);

	public static PurchaseAction CreateQuotedPurchase(
		StarMap map,
		ActorRuntime runtime,
		string unitId,
		MerchantEndpoint endpoint,
		EMerchantCatalog catalog,
		MerchantCatalog.Offering offering,
		MerchantCatalog.Target target,
		ShipInstance before) =>
		CreateQuotedPurchase(
			map,
			runtime,
			unitId,
			endpoint,
			catalog,
			offering,
			target,
			before,
			runtime);

	public static PurchaseAction CreateQuotedPurchase(
		StarMap map,
		ActorRuntime runtime,
		string unitId,
		MerchantEndpoint endpoint,
		EMerchantCatalog catalog,
		MerchantCatalog.Offering offering,
		MerchantCatalog.Target target,
		ShipInstance before,
		ActorRuntime pricingRuntime)
	{
		var pricing = FetchMerchantCatalogActionDef.CreatePricingProbe(
			unitId,
			endpoint,
			catalog,
			before);
		var offer = FetchMerchantCatalogActionDef.PriceCatalog(
				pricing,
				map,
				pricingRuntime)
			.Quote
			.Offers
			.FirstOrDefault(candidate =>
				candidate.Offering == offering && candidate.Targets.Contains(target));
		if (offer is null)
			throw new InvalidOperationException("Merchant offer is not available for quoted purchase.");

		return new PurchaseAction(
			unitId,
			endpoint,
			catalog,
			offering,
			target,
			before,
			offer.Cost);
	}
}
