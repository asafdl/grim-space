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
	public static MerchantCatalog.Offering RepairHull => new(MerchantCatalog.Kind.RepairHull);

	public static MerchantCatalog.Offering RechargeAllShields =>
		new(MerchantCatalog.Kind.RechargeAllShields);

	public static MerchantCatalog.Offering BuyFuel => new(MerchantCatalog.Kind.BuyFuel);

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
		if (strategicGrants is not null)
			runtimes.For(unitId).ReplaceStrategicCapabilities(strategicGrants);
		var engine = new Engine<StarMap, ActorRuntime>(map, runtimes, rules: StarMapPilotCapabilities.Rules);
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
		CreateQuotedPurchase(engine.World, unitId, endpoint, catalog, offering, target, before);

	public static PurchaseAction CreateQuotedPurchase(
		StarMap map,
		string unitId,
		MerchantEndpoint endpoint,
		EMerchantCatalog catalog,
		MerchantCatalog.Offering offering,
		MerchantCatalog.Target target,
		ShipInstance before)
	{
		var offer = MerchantCatalog.ListFor(catalog, before, map)
			.FirstOrDefault(candidate =>
				candidate.Offering == offering && candidate.Target == target);
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
