using GrimSpace.Components;

namespace GrimSpace.Pilots;

public static class PilotQuirkCatalog
{
	private static readonly IReadOnlyDictionary<EPilotQuirkId, PilotQuirkDefinition> Definitions =
		new Dictionary<EPilotQuirkId, PilotQuirkDefinition>
		{
			[EPilotQuirkId.Charismatic] = new(
				EPilotQuirkId.Charismatic,
				EPilotQuirkPolarity.Positive,
				[EPilotCapabilityId.FleetSalaryDiscount],
				new HudTooltipContent(
					"Charismatic",
					$"[icon: Salary, text: -{PilotSkillPercents.BeanCounter.FleetSalaryDiscountPercent}%] fleet salary."),
				personalityModifier: new(0, 0, 3)),
			[EPilotQuirkId.PirateHater] = new(
				EPilotQuirkId.PirateHater,
				EPilotQuirkPolarity.Positive,
				[EPilotCapabilityId.PirateDamageBonus],
				new HudTooltipContent(
					"Pirate Hater",
					$"[icon: Damage, text: +{PilotSkillPercents.PirateHater.DamageBonusPercent}%] damage vs pirates.")),
			[EPilotQuirkId.ShieldMaster] = new(
				EPilotQuirkId.ShieldMaster,
				EPilotQuirkPolarity.Positive,
				[EPilotCapabilityId.BaseShieldBonus],
				new HudTooltipContent(
					"Shield Master",
					$"[icon: Shield, text: +{PilotSkillPercents.ShieldMaster.BaseShieldBonusPerFace}] base shields per face.")),
			[EPilotQuirkId.MetalMaster] = new(
				EPilotQuirkId.MetalMaster,
				EPilotQuirkPolarity.Positive,
				[EPilotCapabilityId.BaseHullBonus],
				new HudTooltipContent(
					"Metal Master",
					$"[icon: Hull, text: +{PilotSkillPercents.MetalMaster.BaseHullBonus}] base hull.")),
			[EPilotQuirkId.ICanFixThat] = new(
				EPilotQuirkId.ICanFixThat,
				EPilotQuirkPolarity.Positive,
				[EPilotCapabilityId.HealBonus],
				new HudTooltipContent(
					"I Can Fix That!",
					$"[icon: Heal, text: +{PilotSkillPercents.ICanFixThat.HealBonusPercent}%] healing.")),
			[EPilotQuirkId.TeamPlayer] = new(
				EPilotQuirkId.TeamPlayer,
				EPilotQuirkPolarity.Positive,
				[],
				new HudTooltipContent(
					"Team Player",
					"This bot will do what it takes to keep the mercs alive."),
				personalityModifier: new(0, 0, 8)),
			[EPilotQuirkId.IceGears] = new(
				EPilotQuirkId.IceGears,
				EPilotQuirkPolarity.Positive,
				[EPilotCapabilityId.HeatGainReduction],
				new HudTooltipContent(
					"Ice Gears",
					$"[icon: Heat, text: -{PilotSkillPercents.PodRacer.HeatGainReductionPercent}%] generation.")),
			[EPilotQuirkId.RadiatorFanatic] = new(
				EPilotQuirkId.RadiatorFanatic,
				EPilotQuirkPolarity.Positive,
				[EPilotCapabilityId.HeatClearBonus],
				new HudTooltipContent(
					"Radiator Fanatic",
					$"[icon: Heat, text: +{PilotSkillPercents.RadiatorFanatic.HeatClearBonusPercent}%] cleared / turn.")),
			[EPilotQuirkId.NeuroDrugAddict] = new(
				EPilotQuirkId.NeuroDrugAddict,
				EPilotQuirkPolarity.Negative,
				[],
				new HudTooltipContent(
					"Neuro-drug Addict",
					"Prone to self centered and compulsive behavior."),
				personalityModifier: new(0, -8, -6)),
			[EPilotQuirkId.BadTechnician] = new(
				EPilotQuirkId.BadTechnician,
				EPilotQuirkPolarity.Negative,
				[EPilotCapabilityId.BaseShieldPenalty],
				new HudTooltipContent(
					"Bad Technician",
					$"[icon: Shield, text: -{PilotSkillPercents.BadTechnician.BaseShieldPenaltyPerFace}] base shields per face.")),
			[EPilotQuirkId.WeakWelding] = new(
				EPilotQuirkId.WeakWelding,
				EPilotQuirkPolarity.Negative,
				[EPilotCapabilityId.BaseHullFlatPenalty],
				new HudTooltipContent(
					"Weak Welding",
					$"[icon: Hull, text: -{PilotSkillPercents.WeakWelding.BaseHullPenalty}] base hull.")),
			[EPilotQuirkId.RoughPatchJob] = new(
				EPilotQuirkId.RoughPatchJob,
				EPilotQuirkPolarity.Negative,
				[EPilotCapabilityId.HealPenalty],
				new HudTooltipContent(
					"Rough Patch Job",
					$"[icon: Heal, text: -{100 - PilotSkillPercents.RoughPatchJob.HealTakenPercent}%] healing.")),
			[EPilotQuirkId.TriggerHappy] = new(
				EPilotQuirkId.TriggerHappy,
				EPilotQuirkPolarity.Negative,
				[],
				new HudTooltipContent(
					"Trigger Happy",
					"Shoots first. Asks never."),
				personalityModifier: new(8, -6, 0)),
			[EPilotQuirkId.Coward] = new(
				EPilotQuirkId.Coward,
				EPilotQuirkPolarity.Negative,
				[],
				new HudTooltipContent(
					"Coward",
					"Self preservation process runs in infinite recursion."),
				personalityModifier: new(-8, 0, -4)),
			[EPilotQuirkId.FurnaceCore] = new(
				EPilotQuirkId.FurnaceCore,
				EPilotQuirkPolarity.Negative,
				[EPilotCapabilityId.HeatGainBonus],
				new HudTooltipContent(
					"Furnace Core",
					$"[icon: Heat, text: +{PilotSkillPercents.SynthSinger.HeatGainBonusPercent}%] generation.")),
			[EPilotQuirkId.CloggedVents] = new(
				EPilotQuirkId.CloggedVents,
				EPilotQuirkPolarity.Negative,
				[EPilotCapabilityId.HeatClearPenalty],
				new HudTooltipContent(
					"Clogged Vents",
					$"[icon: Heat, text: -{100 - PilotSkillPercents.CloggedVents.HeatClearTakenPercent}%] cleared / turn.")),
		};

	public static IReadOnlyList<EPilotQuirkId> Ids { get; } =
		Array.AsReadOnly(Definitions.Keys.ToArray());

	public static PilotQuirkDefinition Get(EPilotQuirkId id) =>
		Definitions.TryGetValue(id, out var definition)
			? definition
			: throw new KeyNotFoundException($"Unknown pilot quirk '{id}'.");

	public static bool TryGet(EPilotQuirkId id, out PilotQuirkDefinition definition) =>
		Definitions.TryGetValue(id, out definition!);

	public static IEnumerable<PilotQuirkDefinition> All => Definitions.Values;

	public static IReadOnlyList<EPilotQuirkId> RollQuirks(Random rng) =>
		RollQuirks(Ids, rng);

	internal static IReadOnlyList<EPilotQuirkId> RollQuirks(
		IReadOnlyList<EPilotQuirkId> pool, Random rng)
	{
		ArgumentNullException.ThrowIfNull(pool);
		ArgumentNullException.ThrowIfNull(rng);
		var remaining = pool.ToList();
		if (remaining.Distinct().Count() != remaining.Count)
			throw new ArgumentException("The quirk pool must not contain duplicates.", nameof(pool));
		foreach (var id in remaining)
			Get(id);

		var count = System.Math.Min(rng.Next(3), remaining.Count);
		var result = new EPilotQuirkId[count];
		for (var index = 0; index < count; index++)
		{
			var pick = rng.Next(remaining.Count);
			result[index] = remaining[pick];
			remaining.RemoveAt(pick);
		}
		return Array.AsReadOnly(result);
	}

	internal static IReadOnlyList<EPilotQuirkId> ValidateQuirks(
		IReadOnlyList<EPilotQuirkId> quirks, EPilotQuirkPolarity polarity)
	{
		ArgumentNullException.ThrowIfNull(quirks);
		if (quirks.Count > 4)
			throw new ArgumentException("A pilot can have at most four quirks per polarity.", nameof(quirks));
		var copy = quirks.ToArray();
		if (copy.Distinct().Count() != copy.Length)
			throw new ArgumentException("A pilot cannot have duplicate quirks.", nameof(quirks));
		foreach (var id in copy)
			if (Get(id).Polarity != polarity)
				throw new ArgumentException($"Quirk '{id}' must have polarity '{polarity}'.", nameof(quirks));
		return Array.AsReadOnly(copy);
	}

	public static EPilotQuirkId Roll(EPilotQuirkPolarity polarity, Random rng)
	{
		ArgumentNullException.ThrowIfNull(rng);
		var options = Definitions.Values
			.Where(definition => definition.Polarity == polarity)
			.Select(definition => definition.Id)
			.ToArray();
		if (options.Length == 0)
			throw new InvalidOperationException($"No {polarity} quirks are registered.");

		return options[rng.Next(options.Length)];
	}
}
