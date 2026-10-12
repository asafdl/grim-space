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

	public static IReadOnlyList<EPilotSkillId> LearnedBattleAbilities(Pilot pilot) =>
		BattleAbilitiesFrom(pilot.LearnedSkills);

	private static IReadOnlyList<EPilotSkillId> BattleAbilitiesFrom(
		IReadOnlyList<EPilotSkillId> skillIds)
	{
		if (skillIds.Count == 0)
			return Array.Empty<EPilotSkillId>();

		var battleSkills = new List<EPilotSkillId>(skillIds.Count);
		foreach (var skillId in skillIds)
		{
			if (PilotSkillCatalog.Get(skillId).Tag != EPilotSkillTag.BattleAbility)
				continue;

			battleSkills.Add(skillId);
		}

		return battleSkills;
	}
}
