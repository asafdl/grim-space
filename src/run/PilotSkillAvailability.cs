using GrimSpace.Pilots;

namespace GrimSpace.Run;

public static class PilotSkillAvailability
{
	public static bool AppliesToPilot(Pilot pilot, PilotSkillDefinition definition) =>
		definition.Audience.Kind switch
		{
			EPilotSkillAudience.Global => true,
			EPilotSkillAudience.OperatorClass =>
				pilot.ClassId == definition.Audience.OperatorClassId,
			EPilotSkillAudience.Background =>
				pilot.BackgroundId == definition.Audience.BackgroundId,
			_ => false,
		};

	public static IEnumerable<PilotSkillDefinition> TreeFor(Pilot pilot)
	{
		var learned = pilot.LearnedSkills.ToHashSet();
		foreach (var definition in PilotSkillCatalog.All)
		{
			if (learned.Contains(definition.Id) || AppliesToPilot(pilot, definition))
				yield return definition;
		}
	}

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
			if (!AppliesToPilot(pilot, definition))
				continue;
			if (!MeetsCatalogGates(pilot, definition))
				continue;

			available.Add(definition.Id);
		}

		return available;
	}

}
