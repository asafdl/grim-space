using GrimSpace.Pilots;
using GrimSpace.Run;
using GrimSpace.Units.Enums;
using GrimSpace.World.StarSystem.Merchants;

namespace GrimSpace.Tests.Run;

[IntegrationTestSuite]
public sealed class PilotHireAcquireTests
{
	[Fact]
	public void TryAcquirePilotHire_RegistersUnassignedInactivePilotWithoutAddingShip()
	{
		using var run = State.CreateNewRun(42);
		var profile = new PilotRecruitProfile(
			"Vera Kite",
			"pilot_03",
			EPilotBackgroundId.PodRacer);
		var ownedShipsBefore = run.ItemInventory.ShipsOwnedBy(State.PlayerFleetUnitId).Count();
		var fieldedShipsBefore = run.GetFieldedShipIds().ToArray();

		Assert.True(run.TryAcquirePilotHire(new PilotHireCommitted(profile)));

		var pilot = Assert.Single(run.PilotRegistry.All, candidate => candidate.Name == profile.Name);
		Assert.Null(pilot.AssignedShipId);
		Assert.False(pilot.IsActive);
		Assert.Equal(ownedShipsBefore, run.ItemInventory.ShipsOwnedBy(State.PlayerFleetUnitId).Count());
		Assert.Equal(fieldedShipsBefore, run.GetFieldedShipIds());
	}

	[Fact]
	public void TryAcquirePilotHire_PreservesRolledOperatorClass()
	{
		using var run = State.CreateNewRun(42);
		var profile = new PilotRecruitProfile(
			"Support Hire",
			"pilot_04",
			EPilotBackgroundId.ScrapDrone,
			classId: EOperatorClassId.Support);

		Assert.True(run.TryAcquirePilotHire(new PilotHireCommitted(profile)));

		var pilot = Assert.Single(run.PilotRegistry.All, candidate => candidate.Name == profile.Name);
		Assert.Equal(EOperatorClassId.Support, pilot.ClassId);
	}
}
