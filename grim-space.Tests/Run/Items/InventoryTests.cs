using GrimSpace.Run.Items;
using GrimSpace.Units;
using GrimSpace.Units.Enums;
using GrimSpace.World.StarSystem.Ids;

namespace GrimSpace.Tests.Run.Items;

[IntegrationTestSuite]
public sealed class InventoryTests
{
	private const string OwnerA = "fleet-a";
	private const string OwnerB = "fleet-b";

	[Fact]
	public void Register_IsNoOpWhenShipAlreadyRegistered()
	{
		var inventory = new Inventory();
		var ship = ShipInstance.FromCatalog("fighter-a", EType.Fighter);

		inventory.Register(ship, OwnerA);
		inventory.Register(ShipInstance.FromCatalog("fighter-a", EType.Fighter), OwnerA);

		Assert.Single(inventory.All);
		Assert.Equal(EType.Fighter, inventory.Get("fighter-a").Spec.Chassis);
	}

	[Fact]
	public void Register_ThrowsWhenSecondCallDiffersByChassis()
	{
		var inventory = new Inventory();
		inventory.Register(ShipInstance.FromCatalog("fighter-a", EType.Fighter), OwnerA);

		Assert.Throws<InvalidOperationException>(() =>
			inventory.Register(ShipInstance.FromCatalog("fighter-a", EType.RepurposedMiner), OwnerA));
	}

	[Fact]
	public void Register_ThrowsWhenSecondCallDiffersByOwner()
	{
		var inventory = new Inventory();
		inventory.Register(ShipInstance.FromCatalog("fighter-a", EType.Fighter), OwnerA);

		Assert.Throws<InvalidOperationException>(() =>
			inventory.Register(ShipInstance.FromCatalog("fighter-a", EType.Fighter), OwnerB));
	}

	[Fact]
	public void Update_ReplacesRegisteredShip()
	{
		var inventory = new Inventory();
		inventory.Register(ShipInstance.FromCatalog("fighter-a", EType.Fighter), OwnerA);
		var upgraded = ShipInstance.FromCatalog("fighter-a", EType.RepurposedMiner);

		inventory.Update(upgraded);

		Assert.Equal(EType.RepurposedMiner, inventory.Get("fighter-a").Spec.Chassis);
		Assert.True(inventory.IsOwnedBy("fighter-a", OwnerA));
	}

	[Fact]
	public void Update_ThrowsWhenShipNotRegistered()
	{
		var inventory = new Inventory();

		Assert.Throws<KeyNotFoundException>(() =>
			inventory.Update(ShipInstance.FromCatalog("missing", EType.Fighter)));
	}

	[Fact]
	public void Remove_DropsRegisteredShip()
	{
		var inventory = new Inventory();
		inventory.Register(ShipInstance.FromCatalog("fighter-a", EType.Fighter), OwnerA);

		inventory.Remove("fighter-a");

		Assert.Empty(inventory.All);
		Assert.False(inventory.TryGet("fighter-a", out _));
	}

	[Fact]
	public void Remove_ThrowsWhenShipNotRegistered()
	{
		var inventory = new Inventory();

		Assert.Throws<KeyNotFoundException>(() => inventory.Remove("missing"));
	}

	[Fact]
	public void Clone_ReflectsRegistryEditsBeforeEnlistment()
	{
		var inventory = new Inventory();
		inventory.Register(ShipInstance.FromCatalog("fighter-a", EType.Fighter), OwnerA);
		inventory.Get("fighter-a").HullPoints = 1;

		var copy = inventory.Get("fighter-a").Clone();

		Assert.Equal(1, copy.HullPoints);
	}

	[Fact]
	public void Matches_ReturnsFalseForMissingShip()
	{
		var inventory = new Inventory();
		var ship = ShipInstance.FromCatalog("fighter-a", EType.Fighter);
		Assert.False(inventory.Matches("fighter-a", ship));
	}

	[Fact]
	public void Matches_ReturnsTrueWhenRegisteredShipMatchesSnapshot()
	{
		var inventory = new Inventory();
		var ship = ShipInstance.FromCatalog("fighter-a", EType.Fighter);
		inventory.Register(ship, OwnerA);
		Assert.True(inventory.Matches("fighter-a", ship.Clone()));
	}

	[Fact]
	public void ApplyHandoff_UpdatesVitals()
	{
		var inventory = new Inventory();
		inventory.Register(ShipInstance.FromCatalog("repurposed-miner-a", EType.RepurposedMiner), OwnerA);
		var shields = ShipCatalog.NewRunLoadoutFor(EType.RepurposedMiner).MaxShieldPoints.Clone();
		shields[GrimSpace.Math.Grid.ESpatialOrientation.Forward] = 1;

		inventory.ApplyHandoff(new GrimSpace.Battle.Objectives.UnitStateHandoff(
			"repurposed-miner-a",
			EType.RepurposedMiner,
			0,
			shields));

		Assert.Equal(0, inventory.Get("repurposed-miner-a").HullPoints);
		Assert.Equal(1, inventory.Get("repurposed-miner-a").ShieldPoints[GrimSpace.Math.Grid.ESpatialOrientation.Forward]);
	}

	[Fact]
	public void OwnershipQueries_ReflectRegisteredOwner()
	{
		var inventory = new Inventory();
		inventory.Register(ShipInstance.FromCatalog("fighter-a", EType.Fighter), StarSystemActorIds.PlayerFleet);
		inventory.Register(ShipInstance.FromCatalog("miner-a", EType.RepurposedMiner), OwnerB);

		Assert.True(inventory.TryGetOwnerId("fighter-a", out var owner));
		Assert.Equal(StarSystemActorIds.PlayerFleet, owner);
		Assert.True(inventory.IsPlayerOwned("fighter-a"));
		Assert.False(inventory.IsPlayerOwned("miner-a"));
		Assert.Equal(["fighter-a"], inventory.ShipsOwnedBy(StarSystemActorIds.PlayerFleet).Select(s => s.Id));
		Assert.Equal(["miner-a"], inventory.ShipsOwnedBy(OwnerB).Select(s => s.Id));
	}

	[Fact]
	public void CaptureRestoreSnapshot_PreservesOwnerId()
	{
		var inventory = new Inventory();
		inventory.Register(ShipInstance.FromCatalog("fighter-a", EType.Fighter), OwnerA);
		inventory.Get("fighter-a").HullPoints = 2;

		var snapshot = inventory.CaptureSnapshot();
		var restored = new Inventory();
		restored.RestoreSnapshot(snapshot);

		Assert.True(restored.IsOwnedBy("fighter-a", OwnerA));
		Assert.Equal(2, restored.Get("fighter-a").HullPoints);
	}
}
