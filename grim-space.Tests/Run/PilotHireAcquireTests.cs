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
	public void TryAcquirePilotHire_WithCompatibleShipActivatesPilotAndFieldsShip()
	{
		using var run = State.CreateNewRun(42);
		var declaration = new ShipSpawnDeclaration("hire-hull-a", BattleUnitType.Fighter);
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
	public void TryAcquirePilotHire_WithUnclassifiedShipKeepsShipOwnedAndPilotUnassigned()
	{
		using var run = State.CreateNewRun(42);
		var declaration = new ShipSpawnDeclaration("hire-hull-unclassified", BattleUnitType.RepurposedMiner);
		var profile = new PilotRecruitProfile(
			"Vera Kite",
			"pilot_03",
			EPilotBackgroundId.PodRacer);

		Assert.True(run.TryAcquirePilotHire(new PilotHireCommitted(profile, declaration)));

		Assert.True(run.ItemInventory.IsOwnedBy(declaration.ShipId, State.PlayerFleetUnitId));
		Assert.DoesNotContain(declaration.ShipId, run.GetFieldedShipIds());
		var pilot = run.PilotRegistry.All.Single(candidate => candidate.Name == "Vera Kite");
		Assert.Null(pilot.AssignedShipId);
		Assert.False(pilot.IsActive);
	}

	[Fact]
	public void TryAcquirePilotHire_WithFullActiveRoster_RegistersInactivePilotAndReserveShip()
	{
		using var run = State.CreateNewRun(42);
		for (var index = 0; index < RunPilotRegistry.MaxActivePilots - 1; index++)
		{
			var shipId = TypedIdGenerator.NextId("fighter");
			Assert.True(run.TryEnlistPlayerShip(new ShipSpawnDeclaration(shipId, BattleUnitType.Fighter)));
		}

		Assert.Equal(RunPilotRegistry.MaxActivePilots, run.PilotRegistry.Active.Count);
		var pilotCountBefore = run.PilotRegistry.All.Count;
		var ownedBefore = run.ItemInventory.ShipsOwnedBy(State.PlayerFleetUnitId).Count();
		var declaration = new ShipSpawnDeclaration("hire-hull-b", BattleUnitType.Fighter);
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
