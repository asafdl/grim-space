using GrimSpace.Pilots;

namespace GrimSpace.Tests.Pilots;

[IntegrationTestSuite]
public sealed class PilotRecruitProfileTests
{
	[Fact]
	public void Constructor_AllowsMissingQuirks()
	{
		var profile = new PilotRecruitProfile(
			"No Quirks",
			"pilot_02",
			EPilotBackgroundId.ScrapDrone);

		Assert.Null(profile.PositiveQuirkId);
		Assert.Null(profile.NegativeQuirkId);
	}

	[Fact]
	public void Constructor_AcceptsLevelZeroProgression()
	{
		var profile = new PilotRecruitProfile(
			"Vera Kite",
			"pilot_03",
			EPilotBackgroundId.PodRacer,
			EPilotQuirkId.Charismatic,
			EPilotQuirkId.NeuroDrugAddict);

		Assert.Equal("Vera Kite", profile.Name);
		Assert.Equal(EPilotBackgroundId.PodRacer, profile.BackgroundId);
		Assert.Equal(0, profile.ExperiencePoints);
		Assert.Equal(1, profile.ClaimedLevel);
		Assert.Empty(profile.LearnedSkills);
	}

	[Fact]
	public void Constructor_RejectsUnknownBackground()
	{
		Assert.Throws<KeyNotFoundException>(() =>
			new PilotRecruitProfile(
				"Bad Hire",
				"pilot_01",
				(EPilotBackgroundId)999,
				EPilotQuirkId.Charismatic,
				EPilotQuirkId.NeuroDrugAddict));
	}

	[Fact]
	public void Constructor_RejectsWrongQuirkPolarity()
	{
		Assert.Throws<ArgumentException>(() =>
			new PilotRecruitProfile(
				"Bad Hire",
				"pilot_01",
				EPilotBackgroundId.ScrapDrone,
				EPilotQuirkId.NeuroDrugAddict,
				EPilotQuirkId.Charismatic));
	}
}
