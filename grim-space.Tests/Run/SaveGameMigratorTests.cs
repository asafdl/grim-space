using GrimSpace.Pilots;
using GrimSpace.Run;
using GrimSpace.Run.Persistence;
using GrimSpace.Units.Enums;

namespace GrimSpace.Tests.Run;

[IntegrationTestSuite]
public sealed class SaveGameMigratorTests
{
	[Fact]
	public void LegacyFormatsRemainUnsupportedWithoutClassMigration()
	{
		foreach (var legacyVersion in new[] { 3, 4, 6 })
		{
			var document = SaveGameDocument.Create("0.1.0", "map", new { marker = "legacy" })
				with { FormatVersion = legacyVersion };

			var unchanged = SaveGameMigrator.MigrateToCurrent(document);

			Assert.Equal(legacyVersion, unchanged.FormatVersion);
			Assert.Equal(
				LoadResult.UnsupportedFormat,
				SaveLoadResult.Classify(SaveStorageResult.Success, unchanged));
		}
	}

	[Fact]
	public void PilotDto_DefaultsSkillProgressionOnRestore()
	{
		var dto = new PilotPersistenceDto(
			"pilot-legacy",
			"Legacy",
			"pilot_01",
			EPilotBackgroundId.ScrapDrone,
			EPilotQuirkId.Charismatic,
			EPilotQuirkId.NeuroDrugAddict,
			"ship-1",
			ExperiencePoints: 175,
			ClassId: EOperatorClassId.SpaceRanger);

		var restored = Pilot.FromPersistence(dto);

		Assert.Equal(PilotProgression.LevelFor(175), restored.ClaimedLevel);
		Assert.Empty(restored.LearnedSkills);
		Assert.Equal(EPilotBackgroundId.ScrapDrone, restored.BackgroundId);
		Assert.Equal(EPilotQuirkId.Charismatic, restored.PositiveQuirkId);
		Assert.Equal(EPilotQuirkId.NeuroDrugAddict, restored.NegativeQuirkId);
		Assert.Equal(EOperatorClassId.SpaceRanger, restored.ClassId);
	}
}
