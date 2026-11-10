using GrimSpace.Tests.Run;
using GrimSpace.Pilots;
using GrimSpace.Run;
using GrimSpace.Run.Persistence;
using GrimSpace.Units.Enums;

namespace GrimSpace.Tests.Run;

[IntegrationTestSuite]
public sealed class PilotIdentityTests
{
	[Fact]
	public void FromPersistence_RejectsInvalidQuirkCollections()
	{
		var dto = Pilot.CreateForShip("ship-1").ToPersistence();
		Assert.Throws<ArgumentNullException>(() =>
			Pilot.FromPersistence(dto with { PositiveQuirkIds = null! }));
		Assert.Throws<ArgumentException>(() =>
			Pilot.FromPersistence(dto with
			{
				NegativeQuirkIds = [EPilotQuirkId.NeuroDrugAddict, EPilotQuirkId.NeuroDrugAddict],
			}));
		Assert.Throws<ArgumentException>(() =>
			Pilot.FromPersistence(dto with
			{
				PositiveQuirkIds = Enumerable.Repeat(EPilotQuirkId.Charismatic, 5).ToArray(),
			}));
	}

	[Fact]
	public void CreateForShip_RollsBackgroundAndQuirksFromCatalog()
	{
		var pilot = Pilot.CreateForShip("fighter-a", rng: new Random(42));

		Assert.StartsWith("robot_v1:", pilot.PortraitId, StringComparison.Ordinal);
		Assert.Equal(pilot.PortraitId, Pilot.CreateForShip("fighter-b", rng: new Random(42)).PortraitId);
		Assert.Contains(PilotBackgroundCatalog.All, background => background.Id == pilot.BackgroundId);
		Assert.Equal(
			EPilotQuirkPolarity.Positive,
			PilotQuirkCatalog.Get(Assert.Single(pilot.PositiveQuirkIds)).Polarity);
		Assert.Equal(
			EPilotQuirkPolarity.Negative,
			PilotQuirkCatalog.Get(Assert.Single(pilot.NegativeQuirkIds)).Polarity);
	}

	[Fact]
	public void FromRecruitProfile_CopiesFrozenIdentityAndStartsInactive()
	{
		var profile = new PilotRecruitProfile(
			"Frozen Hire",
			"pilot_05",
			EPilotBackgroundId.AsteroidSurfer,
			[EPilotQuirkId.Charismatic],
			[EPilotQuirkId.NeuroDrugAddict],
			experiencePoints: 0,
			claimedLevel: 1);
		var pilot = Pilot.FromRecruitProfile("gunship-1", profile);

		Assert.Equal("gunship-1", pilot.AssignedShipId);
		Assert.Equal(profile.Name, pilot.Name);
		Assert.Equal(profile.PortraitId, pilot.PortraitId);
		Assert.Equal(profile.BackgroundId, pilot.BackgroundId);
		Assert.Equal(profile.ClassId, pilot.ClassId);
		Assert.Equal(profile.PositiveQuirkIds, pilot.PositiveQuirkIds);
		Assert.Equal(profile.NegativeQuirkIds, pilot.NegativeQuirkIds);
		Assert.Equal(profile.ExperiencePoints, pilot.ExperiencePoints);
		Assert.Equal(profile.ClaimedLevel, pilot.ClaimedLevel);
	}

	[Theory]
	[InlineData(EOperatorClassId.SpaceRanger)]
	[InlineData(EOperatorClassId.Support)]
	[InlineData(EOperatorClassId.Tank)]
	[InlineData(EOperatorClassId.Rogue)]
	public void CreateStarterForShip_AssignsRolledBackgroundAndScriptedQuirks(EOperatorClassId classId)
	{
		var pilot = Pilot.CreateStarterForShip("starter-a", new Random(42), classId);

		Assert.StartsWith("robot_v1:", pilot.PortraitId, StringComparison.Ordinal);
		Assert.Equal(pilot.PortraitId, Pilot.CreateStarterForShip("starter-b", new Random(42), classId).PortraitId);
		Assert.Contains(PilotBackgroundCatalog.All, background => background.Id == pilot.BackgroundId);
		Assert.Equal(EPilotQuirkId.Charismatic, Assert.Single(pilot.PositiveQuirkIds));
		Assert.Equal(EPilotQuirkId.NeuroDrugAddict, Assert.Single(pilot.NegativeQuirkIds));
		Assert.Equal(classId, pilot.ClassId);
	}

	[Fact]
	public void AddExperience_PreservesIdentity()
	{
		var original = Pilot.CreateForShip("fighter-a");
		var pilot = PilotProgression.AddExperience(original, 250);

		Assert.Equal(original.PositiveQuirkIds, pilot.PositiveQuirkIds);
		Assert.Equal(original.NegativeQuirkIds, pilot.NegativeQuirkIds);
		Assert.Equal(original.ClassId, pilot.ClassId);
	}

	[Fact]
	public void WithAssignment_PreservesIdentity()
	{
		var original = Pilot.CreateForShip("fighter-a");
		var pilot = original.WithAssignment("fighter-b");

		Assert.Equal(original.PositiveQuirkIds, pilot.PositiveQuirkIds);
		Assert.Equal(original.NegativeQuirkIds, pilot.NegativeQuirkIds);
		Assert.Equal(original.ClassId, pilot.ClassId);
	}

	[Fact]
	public void LearnPilotSkill_PreservesIdentity()
	{
		var original = Pilot.CreateForShip("fighter-a");
		var pilot = PilotProgression
			.AddExperience(original, PilotProgression.ExperienceForLevel(3))
			.LearnPilotSkill(EPilotSkillId.ShieldsUp);

		Assert.Equal(original.PositiveQuirkIds, pilot.PositiveQuirkIds);
		Assert.Equal(original.NegativeQuirkIds, pilot.NegativeQuirkIds);
		Assert.Equal(original.ClassId, pilot.ClassId);
	}

	[Fact]
	public void Clone_PreservesIdentity()
	{
		var pilot = Pilot.CreateForShip("fighter-a");
		var clone = pilot.Clone();

		Assert.Equal(pilot.BackgroundId, clone.BackgroundId);
		Assert.Equal(pilot.PositiveQuirkIds, clone.PositiveQuirkIds);
		Assert.Equal(pilot.NegativeQuirkIds, clone.NegativeQuirkIds);
		Assert.Equal(pilot.ClassId, clone.ClassId);
	}

	[Fact]
	public void Persistence_RoundTripsIdentityFields()
	{
		var pilot = PilotProgression.AddExperience(Pilot.CreateForShip("fighter-a"), 175);
		var dto = pilot.ToPersistence();
		var serializedDto = ReflectionJson.Write(dto);
		var restoredDto = ReflectionJson.Read<PilotPersistenceDto>(serializedDto);
		var restored = Pilot.FromPersistence(restoredDto);

		Assert.Equal(pilot.PortraitId, restored.PortraitId);
		Assert.Equal(pilot.BackgroundId, restored.BackgroundId);
		Assert.Equal(pilot.PositiveQuirkIds, restored.PositiveQuirkIds);
		Assert.Equal(pilot.NegativeQuirkIds, restored.NegativeQuirkIds);
		Assert.Equal(pilot.ClassId, restored.ClassId);
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
		Assert.Equal(pilot.PortraitId, restored.PortraitId);
		Assert.Equal(pilot.BackgroundId, restored.BackgroundId);
		Assert.Equal(pilot.PositiveQuirkIds, restored.PositiveQuirkIds);
		Assert.Equal(pilot.NegativeQuirkIds, restored.NegativeQuirkIds);
		Assert.Equal(pilot.ClassId, restored.ClassId);
	}

	[Fact]
	public void FromPersistence_RejectsNegativeQuirkInPositiveSlot()
	{
		var dto = new PilotPersistenceDto(
			"pilot-1",
			"Test",
			"pilot_01",
			EPilotBackgroundId.ScrapDrone,
			[EPilotQuirkId.NeuroDrugAddict],
			[EPilotQuirkId.NeuroDrugAddict],
			"ship-1",
			ExperiencePoints: 0,
			ClaimedLevel: 1,
			LearnedSkills: [],
			DeploymentOrder: -1,
			BaseSalaryCredits: 1,
			MissedSalaryPeriods: 0,
			MissedHazardPeriods: 0,
			ClassId: EOperatorClassId.SpaceRanger,
			Personality: default);

		Assert.Throws<ArgumentException>(() => Pilot.FromPersistence(dto));
	}

	[Fact]
	public void FromPersistence_RejectsUnknownClass()
	{
		var pilot = Pilot.CreateForShip("fighter-a");
		var dto = pilot.ToPersistence() with { ClassId = (EOperatorClassId)999 };

		Assert.Throws<InvalidDataException>(() => Pilot.FromPersistence(dto));
	}

	[Fact]
	public void RegenerateMap_PreservesPilotIdentity()
	{
		using var run = RunTestKit.CreateFighterSupportRun();
		var pilotBefore = RunTestKit.StarterRangerPilot(run);
		Assert.Contains(PilotBackgroundCatalog.All, background => background.Id == pilotBefore.BackgroundId);
		Assert.Equal(EPilotQuirkId.Charismatic, Assert.Single(pilotBefore.PositiveQuirkIds));
		Assert.Equal(EPilotQuirkId.NeuroDrugAddict, Assert.Single(pilotBefore.NegativeQuirkIds));

		run.RegenerateMap(77);

		Assert.True(run.PilotRegistry.TryGet(pilotBefore.Id, out var pilotAfter));
		Assert.Equal(pilotBefore.BackgroundId, pilotAfter.BackgroundId);
		Assert.Equal(pilotBefore.PositiveQuirkIds, pilotAfter.PositiveQuirkIds);
		Assert.Equal(pilotBefore.NegativeQuirkIds, pilotAfter.NegativeQuirkIds);
	}
}
