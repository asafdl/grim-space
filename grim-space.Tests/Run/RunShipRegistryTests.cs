using GrimSpace.Run;
using GrimSpace.Units;
using GrimSpace.Units.Enums;

namespace GrimSpace.Tests.Run;

public sealed class RunShipRegistryTests
{
	[Fact]
	public void Register_IsNoOpWhenShipAlreadyRegistered()
	{
		var registry = new RunShipRegistry();
		var ship = ShipInstance.FromCatalog("fighter-a", EType.Fighter);

		registry.Register(ship);
		registry.Register(ShipInstance.FromCatalog("fighter-a", EType.Fighter));

		Assert.Single(registry.All);
		Assert.Equal(EType.Fighter, registry.Get("fighter-a").Spec.Chassis);
	}

	[Fact]
	public void Register_KeepsFirstEntryWhenSecondCallDiffers()
	{
		var registry = new RunShipRegistry();
		registry.Register(ShipInstance.FromCatalog("fighter-a", EType.Fighter));
		registry.Register(ShipInstance.FromCatalog("fighter-a", EType.Patrol));

		Assert.Equal(EType.Fighter, registry.Get("fighter-a").Spec.Chassis);
	}

	[Fact]
	public void Update_ReplacesRegisteredShip()
	{
		var registry = new RunShipRegistry();
		registry.Register(ShipInstance.FromCatalog("fighter-a", EType.Fighter));
		var upgraded = ShipInstance.FromCatalog("fighter-a", EType.Patrol);

		registry.Update(upgraded);

		Assert.Equal(EType.Patrol, registry.Get("fighter-a").Spec.Chassis);
	}

	[Fact]
	public void Update_ThrowsWhenShipNotRegistered()
	{
		var registry = new RunShipRegistry();

		Assert.Throws<KeyNotFoundException>(() =>
			registry.Update(ShipInstance.FromCatalog("missing", EType.Fighter)));
	}

	[Fact]
	public void Clone_ReflectsRegistryEditsBeforeEnlistment()
	{
		var registry = new RunShipRegistry();
		registry.Register(ShipInstance.FromCatalog("fighter-a", EType.Fighter));
		registry.Get("fighter-a").HullPoints = 1;

		var copy = registry.Get("fighter-a").Clone();

		Assert.Equal(1, copy.HullPoints);
	}

	[Fact]
	public void ApplyHandoff_UpdatesVitals()
	{
		var registry = new RunShipRegistry();
		registry.Register(ShipInstance.FromCatalog("patrol-a", EType.Patrol));
		var shields = ShipCatalog.MaxShieldPointsFor(EType.Patrol).Clone();
		shields[GrimSpace.Math.Grid.ESpatialOrientation.Forward] = 1;

		registry.ApplyHandoff(new GrimSpace.Battle.Objectives.UnitStateHandoff(
			"patrol-a",
			EType.Patrol,
			0,
			shields));

		Assert.Equal(0, registry.Get("patrol-a").HullPoints);
		Assert.Equal(1, registry.Get("patrol-a").ShieldPoints[GrimSpace.Math.Grid.ESpatialOrientation.Forward]);
	}
}
