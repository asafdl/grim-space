using GrimSpace.Pilots;
using GrimSpace.Run;
using GrimSpace.Run.Persistence;

namespace GrimSpace.Tests.Run;

[IntegrationTestSuite]
public sealed class SaveGameMigratorTests
{
	[Fact]
	public void MigrateFromV4_BumpsFormatVersion()
	{
		var document = SaveGameDocument.Create("0.1.0", "map", new { marker = "v4" })
			with { FormatVersion = 4 };

		var migrated = SaveGameMigrator.MigrateToCurrent(document);

		Assert.Equal(SaveGameDocument.CurrentFormatVersion, migrated.FormatVersion);
		Assert.Equal("v4", migrated.Payload.GetProperty("marker").GetString());
	}

	[Fact]
	public void PilotDto_DefaultsSkillProgressionOnRestore()
	{
		var dto = new PilotPersistenceDto(
			"pilot-legacy",
			"Legacy",
			"pilot_01",
			EPilotBackgroundId.ScrapDrone,
			EPilotQuirkId.PlaceholderPositive,
			EPilotQuirkId.PlaceholderNegative,
			"ship-1",
			ExperiencePoints: 175);

		var restored = Pilot.FromPersistence(dto);

		Assert.Equal(PilotProgression.LevelFor(175), restored.ClaimedLevel);
		Assert.Empty(restored.LearnedSkills);
		Assert.Equal(EPilotBackgroundId.ScrapDrone, restored.BackgroundId);
		Assert.Equal(EPilotQuirkId.PlaceholderPositive, restored.PositiveQuirkId);
		Assert.Equal(EPilotQuirkId.PlaceholderNegative, restored.NegativeQuirkId);
	}
}
