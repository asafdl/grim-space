using GrimSpace.Core.Engine;
using GrimSpace.Pilots;
using GrimSpace.Run;
using GrimSpace.Units;
using GrimSpace.Units.Enums;
using GrimSpace.Units.Loadouts.Abilities;
using GrimSpace.Units.Specs;
using GrimSpace.Math.Grid;
using GrimSpace.World.StarSystem;
using GrimSpace.World.StarSystem.Merchants;
using GrimSpace.World.StarSystem.Runtime;
using GrimSpace.World.StarSystem.Skills;
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

	public static (Engine<StarMap, ActorRuntime> engine, string unitId, ShipInstance ship, RunShipRegistry registry) CreateEngine(
		StarMapFixture maps,
		int seed = 42,
		IReadOnlyList<PilotSkillGrant>? strategicGrants = null)
	{
		var registry = new RunShipRegistry();
		var map = StarMap.Create(seed, registry);
		StarSystemTestHarness.AddPlayerFleet(map, State.PlayerFleetUnitId);
		var unitId = State.PlayerFleetUnitId;
		var shipId = map.FleetRegistry.FleetOf(unitId).Members[0].Id;
		var ship = ShipInstance.FromSpec(
			shipId,
			FighterSpec.Instance,
			ShipCatalog.FullFighterLoadout());
		registry.Register(ship);
		map = map.Fork();

		var runtimes = new ActorRuntimes<ActorRuntime>();
		var runtime = runtimes.For(unitId);
		if (strategicGrants is not null)
			runtime.ReplaceActionEffects(StarMapActionEffectPipelineFactory.Build(strategicGrants));
		var engine = new Engine<StarMap, ActorRuntime>(map, runtimes);
		return (engine, unitId, ship, registry);
	}
}
