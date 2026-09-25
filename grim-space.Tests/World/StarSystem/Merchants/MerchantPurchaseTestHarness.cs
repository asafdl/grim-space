using GrimSpace.Core.Engine;
using GrimSpace.Run;
using GrimSpace.Units;
using GrimSpace.Units.Enums;
using GrimSpace.Units.Loadouts.Abilities;
using GrimSpace.Math.Grid;
using GrimSpace.World.StarSystem;
using GrimSpace.World.StarSystem.Merchants;
using GrimSpace.World.StarSystem.Runtime;
using GrimSpace.Tests.World.StarSystem.Traffic;

namespace GrimSpace.Tests.World.StarSystem.Merchants;

internal static class MerchantPurchaseTestHarness
{
	public static MerchantCatalog.Offering FlakPortDamageUpgrade =>
		new(MerchantCatalog.Kind.UpgradeDamage, new AbilityMount(EAbilityKind.Flak, ESpatialOrientation.Port));

	public static MerchantCatalog.Offering RailgunForwardDamageUpgrade =>
		new(MerchantCatalog.Kind.UpgradeDamage, new AbilityMount(EAbilityKind.Railgun, ESpatialOrientation.Forward));

	public static MerchantCatalog.Offering FlakPortRangeUpgrade =>
		new(MerchantCatalog.Kind.UpgradeRange, new AbilityMount(EAbilityKind.Flak, ESpatialOrientation.Port));

	public static MerchantCatalog.Offering RailgunForwardInstall =>
		new(
			MerchantCatalog.Kind.InstallWeapon,
			new AbilityMount(EAbilityKind.Railgun, ESpatialOrientation.Forward));

	public static MerchantCatalog.Offering RepairHull => new(MerchantCatalog.Kind.RepairHull);

	public static MerchantCatalog.Offering RechargeAllShields => new(MerchantCatalog.Kind.RechargeAllShields);

	public static MerchantCatalog.Offering RechargeShieldFace(ESpatialOrientation face) =>
		new(MerchantCatalog.Kind.RechargeShieldFace, Face: face);

	public static (Engine<StarMap, ActorRuntime> engine, string unitId, ShipInstance ship, RunShipRegistry registry) CreateEngine(
		StarMapFixture maps,
		int seed = 42)
	{
		var registry = new RunShipRegistry();
		var map = StarMap.Create(seed, registry);
		StarSystemTestHarness.AddPlayerFleet(map, State.PlayerFleetUnitId);
		var unitId = State.PlayerFleetUnitId;
		var shipId = map.FleetRegistry.FleetOf(unitId).Members[0].Id;
		var ship = ShipInstance.FromCatalog(shipId, EType.Fighter);
		registry.Register(ship);
		map = map.Fork();

		var runtimes = new ActorRuntimes<ActorRuntime>();
		runtimes.For(unitId);
		var engine = new Engine<StarMap, ActorRuntime>(map, runtimes);
		return (engine, unitId, ship, registry);
	}
}
