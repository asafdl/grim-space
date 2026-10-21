using GrimSpace.Core.Ids;
using GrimSpace.Pilots;
using GrimSpace.Run;
using GrimSpace.Units;
using GrimSpace.Units.Enums;
using GrimSpace.World.StarSystem.Merchants;
using BattleUnitType = GrimSpace.Units.Enums.EType;

namespace GrimSpace.Tests.Run;

[IntegrationTestSuite]
public sealed class PilotHireAcquireTests
{
	[Fact]
	public void TryAcquirePilotHire_ActivatesPilotAndFieldsShipWhenRosterHasRoom()
	{
		using var run = State.CreateNewRun(42);
		var declaration = new ShipSpawnDeclaration("hire-hull-a", BattleUnitType.Gunship);
		var profile = new PilotRecruitProfile(
			"Vera Kite",
			"pilot_03",
			EPilotBackgroundId.PodRacer);

		Assert.True(run.TryAcquirePilotHire(new PilotHireCommitted(profile, declaration)));
		Assert.True(run.PilotRegistry.TryGetForShip(declaration.ShipId, out var pilot));
		Assert.True(pilot.IsActive);
		Assert.Contains(declaration.ShipId, run.GetFieldedShipIds());
	}

	[Fact]
	public void TryAcquirePilotHire_WithFullActiveRoster_RegistersInactivePilotAndReserveShip()
	{
		using var run = State.CreateNewRun(42);
		for (var index = 0; index < RunPilotRegistry.MaxActivePilots - 1; index++)
		{
			var shipId = TypedIdGenerator.NextId("gunship");
			Assert.True(run.TryEnlistPlayerShip(new ShipSpawnDeclaration(shipId, BattleUnitType.Gunship)));
		}

		Assert.Equal(RunPilotRegistry.MaxActivePilots, run.PilotRegistry.Active.Count);
		var pilotCountBefore = run.PilotRegistry.All.Count;
		var ownedBefore = run.ItemInventory.ShipsOwnedBy(State.PlayerFleetUnitId).Count();
		var declaration = new ShipSpawnDeclaration("hire-hull-b", BattleUnitType.Gunship);
		var profile = new PilotRecruitProfile(
			"Rook Vale",
			"pilot_05",
			EPilotBackgroundId.ScrapDrone);

		Assert.True(run.TryAcquirePilotHire(new PilotHireCommitted(profile, declaration)));

		Assert.Equal(pilotCountBefore + 1, run.PilotRegistry.All.Count);
		Assert.Equal(ownedBefore + 1, run.ItemInventory.ShipsOwnedBy(State.PlayerFleetUnitId).Count());
		Assert.True(run.PilotRegistry.TryGetForShip(declaration.ShipId, out var pilot));
		Assert.False(pilot.IsActive);
		Assert.DoesNotContain(declaration.ShipId, run.GetFieldedShipIds());
	}
}
