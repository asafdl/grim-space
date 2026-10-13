namespace GrimSpace.Pilots;

public static class PilotSkillCatalog
{
	private static readonly IReadOnlyDictionary<EPilotSkillId, PilotSkillDefinition> Definitions =
		new Dictionary<EPilotSkillId, PilotSkillDefinition>
		{
			[EPilotSkillId.ShieldsUp] = new(
				EPilotSkillId.ShieldsUp,
				EPilotSkillTag.BattleAbility,
				"Shields Up",
				"Boost shield faces until next upkeep; pay AP on the following turn.",
				tier: 1,
				minimumEarnedLevel: 1),
		};

	public static PilotSkillDefinition Get(EPilotSkillId id) =>
		Definitions.TryGetValue(id, out var definition)
			? definition
			: throw new KeyNotFoundException($"Unknown pilot skill '{id}'.");

	public static bool TryGet(EPilotSkillId id, out PilotSkillDefinition definition) =>
		Definitions.TryGetValue(id, out definition!);

	public static IEnumerable<PilotSkillDefinition> All => Definitions.Values;
}
