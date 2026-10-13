using GrimSpace.Pilots;
using GrimSpace.Run;

namespace GrimSpace.Tests.Run;

[IntegrationTestSuite]
public sealed class PilotSkillRulesTests
{
	[Fact]
	public void AddExperience_DoesNotLearnOrChangeClaimedLevel()
	{
		var pilot = Pilot.CreateForShip("fighter-a");
		var beforeClaimed = pilot.ClaimedLevel;

		pilot = PilotProgression.AddExperience(pilot, 250);

		Assert.Equal(beforeClaimed, pilot.ClaimedLevel);
		Assert.Empty(pilot.LearnedSkills);
		Assert.True(
			PilotProgression.LevelFor(pilot.ExperiencePoints) > beforeClaimed);
	}

	[Fact]
	public void LearnPilotSkill_AppendsSkillAndConsumesClaim()
	{
		var pilot = PilotProgression.AddExperience(Pilot.CreateForShip("fighter-a"), 100);
		Assert.Contains(EPilotSkillId.ShieldsUp, PilotSkillAvailability.AvailableToLearn(pilot));

		pilot = pilot.LearnPilotSkill(EPilotSkillId.ShieldsUp);

		Assert.Equal([EPilotSkillId.ShieldsUp], pilot.LearnedSkills);
		Assert.Equal(
			PilotProgression.LevelFor(pilot.ExperiencePoints),
			pilot.ClaimedLevel);
		Assert.Empty(PilotSkillAvailability.AvailableToLearn(pilot));
	}

	[Fact]
	public void RunState_LearnsAvailableSkillAndUpdatesRegistry()
	{
		using var run = State.CreateNewRun(42);
		var pilot = Assert.Single(run.PilotRegistry.All);
		run.PilotRegistry.Update(PilotProgression.AddExperience(pilot, 100));

		Assert.True(run.TryLearnPilotSkill(pilot.Id, EPilotSkillId.ShieldsUp));
		Assert.True(run.PilotRegistry.TryGet(pilot.Id, out var updated));
		Assert.Contains(EPilotSkillId.ShieldsUp, updated.LearnedSkills);
		Assert.False(run.TryLearnPilotSkill(pilot.Id, EPilotSkillId.ShieldsUp));
	}

	[Fact]
	public void LearnPilotSkill_ThrowsWhenNoPendingClaim()
	{
		var pilot = Pilot.CreateForShip("fighter-a");

		Assert.Throws<InvalidOperationException>(() =>
			pilot.LearnPilotSkill(EPilotSkillId.ShieldsUp));
	}

	[Fact]
	public void LearnPilotSkill_ThrowsWhenAlreadyLearned()
	{
		var pilot = PilotProgression
			.AddExperience(Pilot.CreateForShip("fighter-a"), 100)
			.LearnPilotSkill(EPilotSkillId.ShieldsUp);

		pilot = PilotProgression.AddExperience(pilot, 150);

		Assert.Throws<InvalidOperationException>(() =>
			pilot.LearnPilotSkill(EPilotSkillId.ShieldsUp));
	}

	[Fact]
	public void LearnedBattleSkills_IncludesAbilitiesAndPassives()
	{
		var pilot = PilotProgression
			.AddExperience(Pilot.CreateForShip("fighter-a"), 250)
			.LearnPilotSkill(EPilotSkillId.ShieldsUp)
			.LearnPilotSkill(EPilotSkillId.IonizedPayload);

		Assert.Equal(
			[EPilotSkillId.ShieldsUp, EPilotSkillId.IonizedPayload],
			PilotSkillAvailability.LearnedBattleSkills(pilot));
	}

	[Fact]
	public void StrategicPassives_RespectLevelGatesAndStayOutOfBattleSnapshots()
	{
		var pilot = PilotProgression.AddExperience(Pilot.CreateForShip("fighter-a"), 100);

		Assert.Contains(
			EPilotSkillId.DronesYearnForMines,
			PilotSkillAvailability.AvailableToLearn(pilot));
		Assert.DoesNotContain(
			EPilotSkillId.IKnowABot,
			PilotSkillAvailability.AvailableToLearn(pilot));

		pilot = pilot.LearnPilotSkill(EPilotSkillId.DronesYearnForMines);

		Assert.Empty(PilotSkillAvailability.LearnedBattleSkills(pilot));

		pilot = PilotProgression.AddExperience(pilot, 350);

		Assert.Contains(
			EPilotSkillId.IKnowABot,
			PilotSkillAvailability.AvailableToLearn(pilot));
	}

	[Fact]
	public void Snapshot_RoundTripsLearnedSkills()
	{
		var registry = new RunPilotRegistry();
		var pilot = PilotProgression
			.AddExperience(Pilot.CreateForShip("fighter-a"), 100)
			.LearnPilotSkill(EPilotSkillId.ShieldsUp);
		registry.Register(pilot);

		var restored = new RunPilotRegistry();
		restored.RestoreSnapshot(registry.CaptureSnapshot());
		Assert.True(restored.TryGet(pilot.Id, out var roundTripped));
		Assert.Equal(pilot.LearnedSkills, roundTripped.LearnedSkills);
		Assert.Equal(pilot.ClaimedLevel, roundTripped.ClaimedLevel);
	}
}
