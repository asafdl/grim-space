using GrimSpace.Pilots;
using GrimSpace.Units.Enums;

namespace GrimSpace.Tests.Pilots;

[IntegrationTestSuite]
public sealed class PilotRecruitProfileTests
{
	[Fact]
	public void Constructor_DefensivelyOwnsQuirkCollections()
	{
		var positive = new[] { EPilotQuirkId.Charismatic };
		var negative = new[] { EPilotQuirkId.NeuroDrugAddict };
		var profile = new PilotRecruitProfile(
			"Test", "pilot_01", EPilotBackgroundId.ScrapDrone, positive, negative);
		positive[0] = EPilotQuirkId.NeuroDrugAddict;
		negative[0] = EPilotQuirkId.Charismatic;

		Assert.Equal(EPilotQuirkId.Charismatic, Assert.Single(profile.PositiveQuirkIds));
		Assert.Equal(EPilotQuirkId.NeuroDrugAddict, Assert.Single(profile.NegativeQuirkIds));
		Assert.Throws<NotSupportedException>(() =>
			((IList<EPilotQuirkId>)profile.PositiveQuirkIds)[0] = EPilotQuirkId.NeuroDrugAddict);
	}

	[Fact]
	public void Constructor_RejectsDuplicateUnknownAndOverCapacityQuirks()
	{
		Assert.Throws<ArgumentException>(() => new PilotRecruitProfile(
			"Test", "pilot_01", EPilotBackgroundId.ScrapDrone,
			[EPilotQuirkId.Charismatic, EPilotQuirkId.Charismatic]));
		Assert.Throws<KeyNotFoundException>(() => new PilotRecruitProfile(
			"Test", "pilot_01", EPilotBackgroundId.ScrapDrone, [(EPilotQuirkId)999]));
		Assert.Throws<ArgumentException>(() => new PilotRecruitProfile(
			"Test", "pilot_01", EPilotBackgroundId.ScrapDrone,
			Enumerable.Repeat(EPilotQuirkId.Charismatic, 5).ToArray()));
	}
	[Fact]
	public void Constructor_AllowsMissingQuirks()
	{
		var profile = new PilotRecruitProfile(
			"No Quirks",
			"pilot_02",
			EPilotBackgroundId.ScrapDrone);

		Assert.Empty(profile.PositiveQuirkIds);
		Assert.Empty(profile.NegativeQuirkIds);
		Assert.Equal(EOperatorClassId.SpaceRanger, profile.ClassId);
	}

	[Fact]
	public void Constructor_RejectsUnclassifiedPilot()
	{
		Assert.Throws<ArgumentOutOfRangeException>(() =>
			new PilotRecruitProfile(
				"Unclassified",
				"pilot_01",
				EPilotBackgroundId.ScrapDrone,
				classId: EOperatorClassId.Unclassified));
	}

	[Fact]
	public void Constructor_AcceptsLevelZeroProgression()
	{
		var profile = new PilotRecruitProfile(
			"Vera Kite",
			"pilot_03",
			EPilotBackgroundId.PodRacer,
			[EPilotQuirkId.Charismatic],
			[EPilotQuirkId.NeuroDrugAddict]);

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
				[EPilotQuirkId.Charismatic],
				[EPilotQuirkId.NeuroDrugAddict]));
	}

	[Fact]
	public void Constructor_RejectsWrongQuirkPolarity()
	{
		Assert.Throws<ArgumentException>(() =>
			new PilotRecruitProfile(
				"Bad Hire",
				"pilot_01",
				EPilotBackgroundId.ScrapDrone,
				[EPilotQuirkId.NeuroDrugAddict],
				[EPilotQuirkId.Charismatic]));
	}
}
