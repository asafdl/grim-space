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
				"Boost shield faces until your next turn; pay AP when that turn begins.",
				tier: 1,
				minimumEarnedLevel: 1),
			[EPilotSkillId.DodgeThis] = new(
				EPilotSkillId.DodgeThis,
				EPilotSkillTag.BattlePassive,
				"Dodge This",
				"Deal 2 additional damage when hitting an adjacent target.",
				tier: 1,
				minimumEarnedLevel: 2),
			[EPilotSkillId.IonizedPayload] = new(
				EPilotSkillId.IonizedPayload,
				EPilotSkillTag.BattlePassive,
				"Ionized Payload",
				"Lightning weapons deal 1 additional damage to shields.",
				tier: 2,
				minimumEarnedLevel: 3),
			[EPilotSkillId.DefensiveManeuvers] = new(
				EPilotSkillId.DefensiveManeuvers,
				EPilotSkillTag.BattlePassive,
				"Defensive Maneuvers",
				"If you spend no maneuver points during your turn, gain a 25% chance to deflect 1 damage from each hit until your next turn ends.",
				tier: 2,
				minimumEarnedLevel: 3),
			[EPilotSkillId.InTheFray] = new(
				EPilotSkillId.InTheFray,
				EPilotSkillTag.BattlePassive,
				"In The Fray",
				"Take 50% less damage from allied units, rounded up. Self-damage is unaffected.",
				tier: 3,
				minimumEarnedLevel: 4),
			[EPilotSkillId.DronesYearnForMines] = new(
				EPilotSkillId.DronesYearnForMines,
				EPilotSkillTag.StrategicPassive,
				"The Drones Yearn for the Mines",
				"Gain 15% more Scrap Alloy from destroyed ships and successful wreckage investigations.",
				tier: 1,
				minimumEarnedLevel: 2),
			[EPilotSkillId.IKnowABot] = new(
				EPilotSkillId.IKnowABot,
				EPilotSkillTag.StrategicPassive,
				"I Know a Bot",
				"Ship upgrades cost 10% fewer Credits and Scrap Alloy. Industrial Core costs are unchanged.",
				tier: 3,
				minimumEarnedLevel: 4),
		};

	public static PilotSkillDefinition Get(EPilotSkillId id) =>
		Definitions.TryGetValue(id, out var definition)
			? definition
			: throw new KeyNotFoundException($"Unknown pilot skill '{id}'.");

	public static bool TryGet(EPilotSkillId id, out PilotSkillDefinition definition) =>
		Definitions.TryGetValue(id, out definition!);

	public static IEnumerable<PilotSkillDefinition> All => Definitions.Values;
}
