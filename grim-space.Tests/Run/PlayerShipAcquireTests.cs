using GrimSpace.Core.Ids;
using GrimSpace.Run;
using GrimSpace.Units;
using GrimSpace.World.StarSystem.Actions;
using BattleUnitType = GrimSpace.Units.Enums.EType;
using RunState = GrimSpace.Run.State;

namespace GrimSpace.Tests.Run;

[IntegrationTestSuite]
public sealed class PlayerShipAcquireTests
{
	[Fact]
	public void TryAcquirePlayerShip_RejectsDuplicateInventoryEntryWithoutRegisteringPilot()
	{
		using var run = RunState.CreateNewRun(42);
		var declaration = new ShipSpawnDeclaration("duplicate-ship", BattleUnitType.Gunship);
		run.ItemInventory.Register(declaration, RunState.PlayerFleetUnitId);
		var pilotCountBefore = run.PilotRegistry.All.Count;

		Assert.False(run.TryAcquirePlayerShip(declaration));
		Assert.Equal(pilotCountBefore, run.PilotRegistry.All.Count);
		Assert.False(run.PilotRegistry.TryGetForShip(declaration.ShipId, out _));
	}

	[Fact]
	public void TryEnlistPlayerShip_SyncsWorldFleetMembersToFieldedShips()
	{
		using var run = RunState.CreateNewRun(42);
		var secondId = TypedIdGenerator.NextId("gunship");
		Assert.True(run.TryEnlistPlayerShip(new ShipSpawnDeclaration(secondId, BattleUnitType.Gunship)));

		var worldFleet = run.StarSystem.Map.FleetRegistry.FleetOf(RunState.PlayerFleetUnitId);
		Assert.Equal(run.GetFieldedShipIds(), worldFleet.Members.Select(member => member.Id).ToArray());
		Assert.Equal(
			run.GetFieldedShipIds(),
			worldFleet.Registrations.Select(declaration => declaration.ShipId).ToArray());
	}

	[Fact]
	public void CanAcquirePlayerShip_BlocksWhenActivePilotCapWouldBeExceededOnMap()
	{
		using var run = RunState.CreateNewRun(42);
		for (var index = 0; index < RunPilotRegistry.MaxActivePilots - 1; index++)
		{
			var shipId = TypedIdGenerator.NextId("gunship");
			Assert.True(run.TryEnlistPlayerShip(new ShipSpawnDeclaration(shipId, BattleUnitType.Gunship)));
		}

		var blockedId = TypedIdGenerator.NextId("gunship");
		Assert.False(run.CanAcquirePlayerShip(new ShipSpawnDeclaration(blockedId, BattleUnitType.Gunship)));
	}
}
