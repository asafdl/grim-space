using GrimSpace.Run;
using GrimSpace.Run.Persistence;
using GrimSpace.Tests.World.StarSystem.Traffic;
using GrimSpace.Units;
using GrimSpace.World.Factions;
using GrimSpace.World.StarSystem;
using BattleUnitType = GrimSpace.Units.Enums.EType;
using RunState = GrimSpace.Run.State;

namespace GrimSpace.Tests.Run;

[IntegrationTestSuite]
public sealed class RunPersistenceMigrationTests
{
	[Fact]
	public void CaptureSnapshot_OmitsLegacyPartyShipIds()
	{
		using var run = RunState.CreateNewRun(42);
		var snapshot = run.CaptureSnapshot(PersistenceRegistry.CreateDefault());

		Assert.Null(snapshot.PartyShipIds);
		Assert.Contains(snapshot.Pilots, pilot => pilot.IsActive);
	}

	[Fact]
	public void FromSnapshot_RestoresActiveRosterFromPilotPersistence()
	{
		using var run = RunState.CreateNewRun(42);
		var secondId = GrimSpace.Core.Ids.TypedIdGenerator.NextId("gunship");
		Assert.True(run.TryEnlistPlayerShip(new ShipSpawnDeclaration(secondId, BattleUnitType.Gunship)));
		var registry = PersistenceRegistry.CreateDefault();

		using var restored = RunState.FromSnapshot(run.CaptureSnapshot(registry), registry);

		Assert.Equal(run.GetFieldedShipIds(), restored.GetFieldedShipIds());
	}

	[Fact]
	public void FromSnapshot_LegacyPartyShipIds_ActivatesPilotsWhenPersistenceLacksActiveFlags()
	{
		using var run = RunState.CreateNewRun(42);
		var registry = PersistenceRegistry.CreateDefault();
		var fielded = run.GetFieldedShipIds();
		var snapshot = run.CaptureSnapshot(registry) with
		{
			Pilots = run.PilotRegistry.CaptureSnapshot()
				.Select(dto => dto with { IsActive = false, ActiveOrder = -1 })
				.ToArray(),
			PartyShipIds = fielded,
		};

		using var restored = RunState.FromSnapshot(snapshot, registry);

		Assert.Equal(fielded, restored.GetFieldedShipIds());
	}

	[Fact]
	public void FromSnapshot_ReassignsLegacyEnemyShipsInferredFromFleetMembership()
	{
		using var run = RunState.CreateNewRun(42);
		var map = run.StarSystem.Map;
		var pirateFleet = StarSystemTestHarness.CreatePirateFleet(
			"pirate-a",
			new GrimSpace.Math.Grid.Coord(4, 0, 0),
			EFaction.Pirates);
		map.FleetRegistry.Add(pirateFleet);
		var enemyShipId = pirateFleet.Members[0].Id;
		run.ItemInventory.Register(
			ShipInstance.FromCatalog(enemyShipId, BattleUnitType.Fighter),
			RunState.PlayerFleetUnitId);

		var registry = PersistenceRegistry.CreateDefault();
		var snapshot = run.CaptureSnapshot(registry) with
		{
			Ships = run.ItemInventory.CaptureSnapshot()
				.Select(entry => SaveDtoMapper.CaptureShip(entry.Ship, ownerId: null))
				.ToArray(),
		};

		using var restored = RunState.FromSnapshot(snapshot, registry);

		Assert.True(restored.ItemInventory.IsOwnedBy(enemyShipId, pirateFleet.State.Id));
		Assert.True(restored.ItemInventory.IsPlayerOwned(run.GetFieldedShipIds()[0]));
	}
}
