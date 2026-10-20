using GrimSpace.Pilots;
using GrimSpace.Run;
using GrimSpace.Run.Persistence;

namespace GrimSpace.Tests.Run;

[IntegrationTestSuite]
public sealed class PilotIdentityTests
{
	[Fact]
	public void CreateForShip_AssignsPlaceholderIdentity()
	{
		var pilot = Pilot.CreateForShip("fighter-a");

		Assert.Equal(EPilotBackgroundId.Unspecified, pilot.BackgroundId);
		Assert.Equal(EPilotQuirkId.PlaceholderPositive, pilot.PositiveQuirkId);
		Assert.Equal(EPilotQuirkId.PlaceholderNegative, pilot.NegativeQuirkId);
	}

	[Fact]
	public void CreateStarterForShip_AssignsScriptedIdentity()
	{
		var pilot = Pilot.CreateStarterForShip("fighter-a");

		Assert.Equal(EPilotBackgroundId.ScrapDrone, pilot.BackgroundId);
		Assert.Equal(EPilotQuirkId.Charismatic, pilot.PositiveQuirkId);
		Assert.Equal(EPilotQuirkId.NeuroDrugAddict, pilot.NegativeQuirkId);
	}

	[Fact]
	public void AddExperience_PreservesIdentity()
	{
		var pilot = PilotProgression.AddExperience(Pilot.CreateForShip("fighter-a"), 250);

		Assert.Equal(EPilotBackgroundId.Unspecified, pilot.BackgroundId);
		Assert.Equal(EPilotQuirkId.PlaceholderPositive, pilot.PositiveQuirkId);
		Assert.Equal(EPilotQuirkId.PlaceholderNegative, pilot.NegativeQuirkId);
	}

	[Fact]
	public void WithAssignment_PreservesIdentity()
	{
		var pilot = Pilot.CreateForShip("fighter-a").WithAssignment("fighter-b", true);

		Assert.Equal(EPilotBackgroundId.Unspecified, pilot.BackgroundId);
		Assert.Equal(EPilotQuirkId.PlaceholderPositive, pilot.PositiveQuirkId);
		Assert.Equal(EPilotQuirkId.PlaceholderNegative, pilot.NegativeQuirkId);
	}

	[Fact]
	public void LearnPilotSkill_PreservesIdentity()
	{
		var pilot = PilotProgression
			.AddExperience(Pilot.CreateForShip("fighter-a"), 100)
			.LearnPilotSkill(EPilotSkillId.ShieldsUp);

		Assert.Equal(EPilotBackgroundId.Unspecified, pilot.BackgroundId);
		Assert.Equal(EPilotQuirkId.PlaceholderPositive, pilot.PositiveQuirkId);
		Assert.Equal(EPilotQuirkId.PlaceholderNegative, pilot.NegativeQuirkId);
	}

	[Fact]
	public void Clone_PreservesIdentity()
	{
		var pilot = Pilot.CreateForShip("fighter-a");
		var clone = pilot.Clone();

		Assert.Equal(pilot.BackgroundId, clone.BackgroundId);
		Assert.Equal(pilot.PositiveQuirkId, clone.PositiveQuirkId);
		Assert.Equal(pilot.NegativeQuirkId, clone.NegativeQuirkId);
	}

	[Fact]
	public void Persistence_RoundTripsIdentityFields()
	{
		var pilot = PilotProgression.AddExperience(Pilot.CreateForShip("fighter-a"), 175);
		var dto = pilot.ToPersistence();
		var restored = Pilot.FromPersistence(dto);

		Assert.Equal(pilot.BackgroundId, restored.BackgroundId);
		Assert.Equal(pilot.PositiveQuirkId, restored.PositiveQuirkId);
		Assert.Equal(pilot.NegativeQuirkId, restored.NegativeQuirkId);
	}

	[Fact]
	public void RegistrySnapshot_RoundTripsIdentityFields()
	{
		var registry = new RunPilotRegistry();
		var pilot = Pilot.CreateForShip("fighter-test-ship");
		registry.Register(pilot);

		var dto = Assert.Single(registry.CaptureSnapshot());
		var restoredRegistry = new RunPilotRegistry();
		restoredRegistry.RestoreSnapshot([dto]);

		Assert.True(restoredRegistry.TryGet(pilot.Id, out var restored));
		Assert.Equal(EPilotBackgroundId.Unspecified, restored.BackgroundId);
		Assert.Equal(EPilotQuirkId.PlaceholderPositive, restored.PositiveQuirkId);
		Assert.Equal(EPilotQuirkId.PlaceholderNegative, restored.NegativeQuirkId);
	}

	[Fact]
	public void FromPersistence_RejectsNegativeQuirkInPositiveSlot()
	{
		var dto = new PilotPersistenceDto(
			"pilot-1",
			"Test",
			"pilot_01",
			EPilotBackgroundId.Unspecified,
			EPilotQuirkId.PlaceholderNegative,
			EPilotQuirkId.PlaceholderNegative,
			"ship-1");

		Assert.Throws<ArgumentException>(() => Pilot.FromPersistence(dto));
	}

	[Fact]
	public void RegenerateMap_PreservesPilotIdentity()
	{
		using var run = State.CreateNewRun(42);
		var pilotBefore = Assert.Single(run.PilotRegistry.All);
		Assert.Equal(EPilotBackgroundId.ScrapDrone, pilotBefore.BackgroundId);
		Assert.Equal(EPilotQuirkId.Charismatic, pilotBefore.PositiveQuirkId);
		Assert.Equal(EPilotQuirkId.NeuroDrugAddict, pilotBefore.NegativeQuirkId);

		run.RegenerateMap(77);

		Assert.True(run.PilotRegistry.TryGet(pilotBefore.Id, out var pilotAfter));
		Assert.Equal(pilotBefore.BackgroundId, pilotAfter.BackgroundId);
		Assert.Equal(pilotBefore.PositiveQuirkId, pilotAfter.PositiveQuirkId);
		Assert.Equal(pilotBefore.NegativeQuirkId, pilotAfter.NegativeQuirkId);
	}
}
