using GrimSpace.Run;
using GrimSpace.Units;
using GrimSpace.Units.Enums;

namespace GrimSpace.Tests.Run;

[IntegrationTestSuite]
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
		registry.Register(ShipInstance.FromCatalog("fighter-a", EType.RepurposedMiner));

		Assert.Equal(EType.Fighter, registry.Get("fighter-a").Spec.Chassis);
	}

	[Fact]
	public void Update_ReplacesRegisteredShip()
	{
		var registry = new RunShipRegistry();
		registry.Register(ShipInstance.FromCatalog("fighter-a", EType.Fighter));
		var upgraded = ShipInstance.FromCatalog("fighter-a", EType.RepurposedMiner);

		registry.Update(upgraded);

		Assert.Equal(EType.RepurposedMiner, registry.Get("fighter-a").Spec.Chassis);
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
	public void Matches_ReturnsFalseForMissingShip()
	{
		var registry = new RunShipRegistry();
		var ship = ShipInstance.FromCatalog("fighter-a", EType.Fighter);
		Assert.False(registry.Matches("fighter-a", ship));
	}

	[Fact]
	public void Matches_ReturnsTrueWhenRegisteredShipMatchesSnapshot()
	{
		var registry = new RunShipRegistry();
		var ship = ShipInstance.FromCatalog("fighter-a", EType.Fighter);
		registry.Register(ship);
		Assert.True(registry.Matches("fighter-a", ship.Clone()));
	}

	[Fact]
	public void ApplyHandoff_UpdatesVitals()
	{
		var registry = new RunShipRegistry();
		registry.Register(ShipInstance.FromCatalog("repurposed-miner-a", EType.RepurposedMiner));
		var shields = ShipCatalog.NewRunLoadoutFor(EType.RepurposedMiner).MaxShieldPoints.Clone();
		shields[GrimSpace.Math.Grid.ESpatialOrientation.Forward] = 1;

		registry.ApplyHandoff(new GrimSpace.Battle.Objectives.UnitStateHandoff(
			"repurposed-miner-a",
			EType.RepurposedMiner,
			0,
			shields));

		Assert.Equal(0, registry.Get("repurposed-miner-a").HullPoints);
		Assert.Equal(1, registry.Get("repurposed-miner-a").ShieldPoints[GrimSpace.Math.Grid.ESpatialOrientation.Forward]);
	}
}
