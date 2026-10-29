using GrimSpace.Components;
using GrimSpace.Units.Enums;

namespace GrimSpace.Pilots;

public static class PilotSkillCatalog
{
	private static readonly IReadOnlyDictionary<EPilotSkillId, PilotSkillDefinition> Definitions =
		new Dictionary<EPilotSkillId, PilotSkillDefinition>
		{
			[EPilotSkillId.ReinforcedHull] = new(
				EPilotSkillId.ReinforcedHull,
				[EPilotCapabilityId.ReinforcedHull],
				new HudTooltipContent(
					"Reinforced Hull",
					$"Take {100 - PilotSkillPercents.ReinforcedHull.HullDamageTakenPercent}% less damage to hull."),
				tier: 1,
				minimumEarnedLevel: 1,
				audience: PilotSkillAudience.Global),
			[EPilotSkillId.ShieldsUp] = new(
				EPilotSkillId.ShieldsUp,
				[EPilotCapabilityId.ShieldBoost],
				new HudTooltipContent(
					"Shields Up",
					$"Boost each shield face by {PilotSkillPercents.ShieldsUp.TemporaryShieldPercentOfFaceMax}% of its maximum until your next turn; pay AP when that turn begins."),
				tier: 2,
				minimumEarnedLevel: 3,
				audience: PilotSkillAudience.Global),
			[EPilotSkillId.OverchargeEngines] = new(
				EPilotSkillId.OverchargeEngines,
				[EPilotCapabilityId.OverchargeEngines],
				new HudTooltipContent(
					"Overcharge Engines",
					$"Requires {PilotSkillPercents.OverchargeEngines.MinimumApToUse} AP to use. Refresh {PilotSkillPercents.OverchargeEngines.RefreshedAp} AP this turn (up to max). {PilotSkillPercents.OverchargeEngines.CooldownTurns}-turn cooldown. Get {PilotSkillPercents.OverchargeEngines.NextTurnApPenalty} less AP next turn."),
				tier: 3,
				minimumEarnedLevel: 4,
				audience: PilotSkillAudience.Global),
			[EPilotSkillId.DodgeThis] = new(
				EPilotSkillId.DodgeThis,
				[EPilotCapabilityId.AdjacentDamageBonus],
				new HudTooltipContent(
					"Dodge This",
					$"Deal {PilotSkillPercents.DodgeThis.AdditionalDamagePercent}% additional damage when hitting a target at {PilotSkillPercents.DodgeThis.BonusRangeCells} km range ({PilotSkillPercents.DodgeThis.BonusHitDamagePercent}% total weapon damage)."),
				tier: 1,
				minimumEarnedLevel: 2,
				audience: PilotSkillAudience.ForOperatorClass(EOperatorClassId.SpaceRanger)),
			[EPilotSkillId.IonizedPayload] = new(
				EPilotSkillId.IonizedPayload,
				[EPilotCapabilityId.LightningShieldDamageBonus],
				new HudTooltipContent(
					"Ionized Payload",
					$"Lightning weapons deal {PilotSkillPercents.IonizedPayload.ShieldDamageBonusPercent}% additional damage to shields."),
				tier: 5,
				minimumEarnedLevel: 6,
				audience: PilotSkillAudience.ForOperatorClass(EOperatorClassId.SpaceRanger)),
			[EPilotSkillId.Brace] = new(
				EPilotSkillId.Brace,
				[EPilotCapabilityId.ShieldBrace],
				new HudTooltipContent(
					"Brace!",
					$"Move {PilotSkillPercents.Brace.ShieldConsolidationPercent}% of each shield face to one face you choose until your next turn. That face can exceed its maximum. Shield damage it absorbs is spread to your other faces when the brace ends."),
				tier: 1,
				minimumEarnedLevel: 2,
				audience: PilotSkillAudience.ForOperatorClass(EOperatorClassId.Support)),
			[EPilotSkillId.DefensiveManeuvers] = new(
				EPilotSkillId.DefensiveManeuvers,
				[EPilotCapabilityId.StationaryDamageDeflection],
				new HudTooltipContent(
					"Defensive Maneuvers",
					$"If you spend no maneuver points during your turn, gain a {PilotSkillPercents.DefensiveManeuvers.DeflectChancePercent}% chance to deflect {PilotSkillPercents.DefensiveManeuvers.DeflectDamageTakenPercent}% damage from each hit until your next turn ends."),
				tier: 2,
				minimumEarnedLevel: 3,
				audience: PilotSkillAudience.ForOperatorClass(EOperatorClassId.Support)),
			[EPilotSkillId.InTheFray] = new(
				EPilotSkillId.InTheFray,
				[EPilotCapabilityId.InTheFray],
				new HudTooltipContent(
					"In The Fray",
					$"If at least {PilotSkillPercents.InTheFray.MinimumDistanceCells} km from at least {PilotSkillPercents.InTheFray.MinimumEnemyCount} enemies, take {100 - PilotSkillPercents.InTheFray.DamageTakenPercent}% less damage from hits."),
				tier: 4,
				minimumEarnedLevel: 5,
				audience: PilotSkillAudience.ForOperatorClass(EOperatorClassId.SpaceRanger)),
			[EPilotSkillId.OneWithTheVoid] = new(
				EPilotSkillId.OneWithTheVoid,
				[EPilotCapabilityId.OneWithTheVoid],
				new HudTooltipContent(
					"One With The Void",
					"Void bombs you launch never damage allies or your own ship."),
				tier: 3,
				minimumEarnedLevel: 4,
				audience: PilotSkillAudience.ForOperatorClass(EOperatorClassId.SpaceRanger)),
			[EPilotSkillId.DronesYearnForMines] = new(
				EPilotSkillId.DronesYearnForMines,
				[EPilotCapabilityId.ScrapRewardBonus],
				new HudTooltipContent(
					"The Drones Yearn for the Mines",
					$"Gain {PilotSkillPercents.DronesYearnForMines.ScrapRewardBonusPercent}% more Scrap Alloy from destroyed ships and successful wreckage investigations."),
				tier: 1,
				minimumEarnedLevel: 2,
				audience: PilotSkillAudience.ForBackground(EPilotBackgroundId.ScrapDrone)),
		};

	public static PilotSkillDefinition Get(EPilotSkillId id) =>
		Definitions.TryGetValue(id, out var definition)
			? definition
			: throw new KeyNotFoundException($"Unknown pilot skill '{id}'.");

	public static bool TryGet(EPilotSkillId id, out PilotSkillDefinition definition) =>
		Definitions.TryGetValue(id, out definition!);

	public static IEnumerable<PilotSkillDefinition> All => Definitions.Values;
}
