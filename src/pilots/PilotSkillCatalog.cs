namespace GrimSpace.Pilots;

public static class PilotSkillCatalog
{
	private static readonly IReadOnlyDictionary<EPilotSkillId, PilotSkillDefinition> Definitions =
		new Dictionary<EPilotSkillId, PilotSkillDefinition>
		{
			[EPilotSkillId.ShieldsUp] = new(
				EPilotSkillId.ShieldsUp,
				[EPilotCapabilityId.ShieldBoost],
				"Shields Up",
				$"Boost each shield face by {PilotSkillPercents.ShieldsUp.TemporaryShieldPercentOfFaceMax}% of its maximum until your next turn; pay AP when that turn begins.",
				tier: 1,
				minimumEarnedLevel: 1),
			[EPilotSkillId.DodgeThis] = new(
				EPilotSkillId.DodgeThis,
				[EPilotCapabilityId.AdjacentDamageBonus],
				"Dodge This",
				$"Deal {PilotSkillPercents.DodgeThis.AdditionalDamagePercent}% additional damage when hitting an adjacent target ({PilotSkillPercents.DodgeThis.AdjacentHitDamagePercent}% total weapon damage).",
				tier: 1,
				minimumEarnedLevel: 2),
			[EPilotSkillId.IonizedPayload] = new(
				EPilotSkillId.IonizedPayload,
				[EPilotCapabilityId.LightningShieldDamageBonus],
				"Ionized Payload",
				$"Lightning weapons deal {PilotSkillPercents.IonizedPayload.ShieldDamageBonusPercent}% additional damage to shields.",
				tier: 2,
				minimumEarnedLevel: 3),
			[EPilotSkillId.DefensiveManeuvers] = new(
				EPilotSkillId.DefensiveManeuvers,
				[EPilotCapabilityId.StationaryDamageDeflection],
				"Defensive Maneuvers",
				$"If you spend no maneuver points during your turn, gain a {PilotSkillPercents.DefensiveManeuvers.DeflectChancePercent}% chance to deflect {PilotSkillPercents.DefensiveManeuvers.DeflectDamageTakenPercent}% damage from each hit until your next turn ends.",
				tier: 2,
				minimumEarnedLevel: 3),
			[EPilotSkillId.InTheFray] = new(
				EPilotSkillId.InTheFray,
				[EPilotCapabilityId.AlliedDamageReduction],
				"In The Fray",
				$"Take {100 - PilotSkillPercents.InTheFray.AlliedDamageTakenPercent}% less damage from allied units. Self-damage is unaffected.",
				tier: 3,
				minimumEarnedLevel: 4),
			[EPilotSkillId.DronesYearnForMines] = new(
				EPilotSkillId.DronesYearnForMines,
				[EPilotCapabilityId.ScrapRewardBonus],
				"The Drones Yearn for the Mines",
				$"Gain {PilotSkillPercents.DronesYearnForMines.ScrapRewardBonusPercent}% more Scrap Alloy from destroyed ships and successful wreckage investigations.",
				tier: 1,
				minimumEarnedLevel: 2),
		};

	public static PilotSkillDefinition Get(EPilotSkillId id) =>
		Definitions.TryGetValue(id, out var definition)
			? definition
			: throw new KeyNotFoundException($"Unknown pilot skill '{id}'.");

	public static bool TryGet(EPilotSkillId id, out PilotSkillDefinition definition) =>
		Definitions.TryGetValue(id, out definition!);

	public static IEnumerable<PilotSkillDefinition> All => Definitions.Values;
}
