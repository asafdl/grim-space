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
				minimumEarnedLevel: 1,
				audience: PilotSkillAudience.Global),
			[EPilotSkillId.BetterRam] = new(
				EPilotSkillId.BetterRam,
				[EPilotCapabilityId.ExperienceGainBonus],
				new HudTooltipContent(
					"Better RAM",
					$"Gain XP {PilotSkillPercents.BetterRam.ExperienceGainBonusPercent}% faster."),
				minimumEarnedLevel: 1,
				audience: PilotSkillAudience.Global),
			[EPilotSkillId.HeatVents] = new(
				EPilotSkillId.HeatVents,
				[EPilotCapabilityId.HeatVents],
				new HudTooltipContent(
					"Heat Vents",
					$"Release {PilotSkillPercents.HeatVents.HeatReleased} heat."),
				minimumEarnedLevel: 3,
				audience: PilotSkillAudience.Global),
			[EPilotSkillId.ShieldsUp] = new(
				EPilotSkillId.ShieldsUp,
				[EPilotCapabilityId.ShieldBoost],
				new HudTooltipContent(
					"Shields Up",
					$"Boost each shield face by {PilotSkillPercents.ShieldsUp.TemporaryShieldPercentOfFaceMax}% of its maximum until your next turn; pay AP when that turn begins."),
				minimumEarnedLevel: 3,
				audience: PilotSkillAudience.Global),
			[EPilotSkillId.OverchargeEngines] = new(
				EPilotSkillId.OverchargeEngines,
				[EPilotCapabilityId.OverchargeEngines],
				new HudTooltipContent(
					"Overcharge Engines",
					$"Refresh {PilotSkillPercents.OverchargeEngines.RefreshedAp} AP this turn (up to max). Get {PilotSkillPercents.OverchargeEngines.NextTurnApPenalty} less AP next turn."),
				minimumEarnedLevel: 4,
				audience: PilotSkillAudience.Global),
			[EPilotSkillId.MarkWeakspot] = new(
				EPilotSkillId.MarkWeakspot,
				[EPilotCapabilityId.MarkWeakspot],
				new HudTooltipContent(
					"Mark Weakspot",
					$"Mark an enemy shield face within {PilotSkillPercents.MarkWeakspot.RangeCells} km (grid distance) for 1 round. All attacks that hit that face deal {PilotSkillPercents.MarkWeakspot.ShieldDamageBonusPercent}% additional shield damage."),
				minimumEarnedLevel: 1,
				audience: PilotSkillAudience.ForOperatorClass(EOperatorClassId.Rogue)),
			[EPilotSkillId.ThirstForScrap] = new(
				EPilotSkillId.ThirstForScrap,
				[EPilotCapabilityId.ThirstForScrap],
				new HudTooltipContent(
					"Thirst For Scrap",
					$"Gain a stacking +{PilotSkillPercents.ThirstForScrap.DamageBonusPercentPerKill}% damage for each enemy killed by this unit or its spawned units this battle."),
				minimumEarnedLevel: 4,
				audience: PilotSkillAudience.ForOperatorClass(EOperatorClassId.Rogue)),
			[EPilotSkillId.YouLittleRat] = new(
				EPilotSkillId.YouLittleRat,
				[EPilotCapabilityId.YouLittleRat],
				new HudTooltipContent(
					"You Little Rat",
					$"Take {100 - PilotSkillPercents.YouLittleRat.DamageTakenPercent}% less damage from attacks originating at least {PilotSkillPercents.YouLittleRat.MinimumDistanceCells} km away (grid distance)."),
				minimumEarnedLevel: 4,
				audience: PilotSkillAudience.ForOperatorClass(EOperatorClassId.Rogue)),
			[EPilotSkillId.NothingButMetal] = new(
				EPilotSkillId.NothingButMetal,
				[EPilotCapabilityId.NothingButMetal],
				new HudTooltipContent(
					"Nothing But Metal",
					$"Deal +{PilotSkillPercents.NothingButMetal.DamageBonusPercent}% damage when hitting only hull."),
				minimumEarnedLevel: 5,
				audience: PilotSkillAudience.ForOperatorClass(EOperatorClassId.Rogue)),
			[EPilotSkillId.NotFullyFormed] = new(
				EPilotSkillId.NotFullyFormed,
				[EPilotCapabilityId.SpawnedUnitDamageBonus],
				new HudTooltipContent(
					"Not Fully Formed",
					$"Deal {PilotSkillPercents.NotFullyFormed.SpawnedUnitDamageBonusPercent}% additional damage against spawned units."),
				minimumEarnedLevel: 6,
				audience: PilotSkillAudience.ForOperatorClass(EOperatorClassId.Rogue)),
			[EPilotSkillId.ShieldDynamo] = new(
				EPilotSkillId.ShieldDynamo,
				[EPilotCapabilityId.ShieldDynamo],
				new HudTooltipContent(
					"Shield Dynamo",
					$"Gain {PilotSkillPercents.ShieldDynamo.ShieldsPerFacePerAp} shields on every face for each AP spent until your next round."),
				minimumEarnedLevel: 3,
				audience: PilotSkillAudience.ForOperatorClass(EOperatorClassId.Rogue)),
			[EPilotSkillId.DodgeThis] = new(
				EPilotSkillId.DodgeThis,
				[EPilotCapabilityId.AdjacentDamageBonus],
				new HudTooltipContent(
					"Dodge This",
					$"Deal {PilotSkillPercents.DodgeThis.AdditionalDamagePercent}% additional damage when hitting a target at {PilotSkillPercents.DodgeThis.BonusRangeCells} km range ({PilotSkillPercents.DodgeThis.BonusHitDamagePercent}% total weapon damage)."),
				minimumEarnedLevel: 3,
				audience: PilotSkillAudience.ForOperatorClass(EOperatorClassId.SpaceRanger)),
			[EPilotSkillId.IonizedPayload] = new(
				EPilotSkillId.IonizedPayload,
				[EPilotCapabilityId.LightningShieldDamageBonus],
				new HudTooltipContent(
					"Ionized Payload",
					$"Lightning weapons deal {PilotSkillPercents.IonizedPayload.ShieldDamageBonusPercent}% additional damage to shields."),
				minimumEarnedLevel: 6,
				audience: PilotSkillAudience.ForOperatorClass(EOperatorClassId.SpaceRanger)),
			[EPilotSkillId.Brace] = new(
				EPilotSkillId.Brace,
				[EPilotCapabilityId.ShieldBrace],
				new HudTooltipContent(
					"Brace!",
					$"Move {PilotSkillPercents.Brace.ShieldConsolidationPercent}% of each shield face to one face you choose until your next turn. That face can exceed its maximum. Shield damage it absorbs is spread to your other faces when the brace ends."),
				minimumEarnedLevel: 2,
				audience: PilotSkillAudience.ForOperatorClass(EOperatorClassId.Support)),
			[EPilotSkillId.DefensiveManeuvers] = new(
				EPilotSkillId.DefensiveManeuvers,
				[EPilotCapabilityId.StationaryDamageDeflection],
				new HudTooltipContent(
					"Defensive Maneuvers",
					$"If you spend no maneuver points during your turn, gain a {PilotSkillPercents.DefensiveManeuvers.DeflectChancePercent}% chance to deflect {PilotSkillPercents.DefensiveManeuvers.DeflectDamageTakenPercent}% damage from each hit until your next turn ends."),
				minimumEarnedLevel: 3,
				audience: PilotSkillAudience.ForOperatorClasses(
					EOperatorClassId.Support,
					EOperatorClassId.Tank)),
			[EPilotSkillId.HelpABotterOut] = new(
				EPilotSkillId.HelpABotterOut,
				[EPilotCapabilityId.HealingSkillRangeBonus],
				new HudTooltipContent(
					"Help a Botter Out",
					$"Healing skills have +{PilotSkillPercents.HelpABotterOut.HealingRangeBonusCells} range."),
				minimumEarnedLevel: 5,
				audience: PilotSkillAudience.ForOperatorClass(EOperatorClassId.Support)),
			[EPilotSkillId.InTheFray] = new(
				EPilotSkillId.InTheFray,
				[EPilotCapabilityId.InTheFray],
				new HudTooltipContent(
					"In The Fray",
					$"If at least {PilotSkillPercents.InTheFray.MinimumDistanceCells} km from at least {PilotSkillPercents.InTheFray.MinimumEnemyCount} enemies, take {100 - PilotSkillPercents.InTheFray.DamageTakenPercent}% less damage from hits."),
				minimumEarnedLevel: 5,
				audience: PilotSkillAudience.ForOperatorClasses(
					EOperatorClassId.SpaceRanger,
					EOperatorClassId.Tank)),
			[EPilotSkillId.ReactiveShields] = new(
				EPilotSkillId.ReactiveShields,
				[EPilotCapabilityId.ReactiveShields],
				new HudTooltipContent(
					"Reactive Shields",
					$"After each hit, gain damage resistance {100 - PilotSkillPercents.ReactiveShields.DamageTakenPercentPerStack}%."),
				minimumEarnedLevel: 3,
				audience: PilotSkillAudience.ForOperatorClass(EOperatorClassId.Tank)),
			[EPilotSkillId.OneWithTheVoid] = new(
				EPilotSkillId.OneWithTheVoid,
				[EPilotCapabilityId.OneWithTheVoid],
				new HudTooltipContent(
					"One With The Void",
					"Void bombs you launch never damage allies or your own ship."),
				minimumEarnedLevel: 4,
				audience: PilotSkillAudience.ForOperatorClass(EOperatorClassId.SpaceRanger)),
			[EPilotSkillId.DronesYearnForMines] = new(
				EPilotSkillId.DronesYearnForMines,
				[EPilotCapabilityId.ScrapRewardBonus],
				new HudTooltipContent(
					"The Drones Yearn for the Mines",
					$"Gain {PilotSkillPercents.DronesYearnForMines.ScrapRewardBonusPercent}% more Scrap Alloy from destroyed ships and successful wreckage investigations."),
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

	/// <summary>Global + class skills for an operator class (excludes background-gated skills).</summary>
	public static IReadOnlyList<EPilotSkillId> IdsForOperatorClass(EOperatorClassId classId)
	{
		if (!Enum.IsDefined(classId) || classId == EOperatorClassId.Unclassified)
			throw new ArgumentOutOfRangeException(nameof(classId), classId, null);

		return All
			.Where(definition => definition.Audience.Kind switch
			{
				EPilotSkillAudience.Global => true,
				EPilotSkillAudience.OperatorClass =>
					definition.Audience.OperatorClassIds!.Contains(classId),
				_ => false,
			})
			.Select(definition => definition.Id)
			.ToArray();
	}
}
