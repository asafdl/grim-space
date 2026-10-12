using System.Text.Json;
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
	public void LegacyPilotDto_DefaultsSkillProgressionOnRestore()
	{
		var legacy = new PilotPersistenceDto(
			"pilot-legacy",
			"Legacy",
			"pilot_01",
			"ship-1",
			ExperiencePoints: 175);

		var restored = Pilot.FromPersistence(legacy);

		Assert.Equal(PilotProgression.LevelFor(175), restored.ClaimedLevel);
		Assert.Empty(restored.LearnedSkills);
	}

	[Fact]
	public void LegacyPilotJson_DefaultsSkillProgressionOnDeserialize()
	{
		const string legacyPilotJson =
			"""
			{
			  "id": "pilot-legacy",
			  "name": "Legacy",
			  "portraitId": "pilot_01",
			  "assignedShipId": "ship-1",
			  "experiencePoints": 175
			}
			""";
		using var node = JsonDocument.Parse(legacyPilotJson);
		var legacyDto = ReflectionJson.Read<PilotPersistenceDto>(node.RootElement);
		var restoredPilot = Pilot.FromPersistence(legacyDto);

		Assert.Equal(PilotProgression.LevelFor(175), restoredPilot.ClaimedLevel);
		Assert.Empty(restoredPilot.LearnedSkills);
	}
}
