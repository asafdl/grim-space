using GrimSpace.Pilots;

namespace GrimSpace.Run;

public static class PilotSkillAvailability
{
	public static bool MeetsCatalogGates(Pilot pilot, PilotSkillDefinition definition)
	{
		if (PilotProgression.LevelFor(pilot.ExperiencePoints) < definition.MinimumEarnedLevel)
			return false;

		foreach (var prerequisite in definition.Prerequisites)
		{
			if (!pilot.LearnedSkills.Contains(prerequisite))
				return false;
		}

		return true;
	}

	public static IReadOnlyList<EPilotSkillId> AvailableToLearn(Pilot pilot)
	{
		var earnedLevel = PilotProgression.LevelFor(pilot.ExperiencePoints);
		if (earnedLevel <= pilot.ClaimedLevel)
			return Array.Empty<EPilotSkillId>();

		var learned = new HashSet<EPilotSkillId>(pilot.LearnedSkills);
		var available = new List<EPilotSkillId>();
		foreach (var definition in PilotSkillCatalog.All)
		{
			if (learned.Contains(definition.Id))
				continue;
			if (!MeetsCatalogGates(pilot, definition))
				continue;

			available.Add(definition.Id);
		}

		return available;
	}

}
