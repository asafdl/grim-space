using GrimSpace.Components;

namespace GrimSpace.Pilots;

public static class PilotBackgroundCatalog
{
	private static readonly IReadOnlyDictionary<EPilotBackgroundId, PilotBackgroundDefinition> Definitions =
		new Dictionary<EPilotBackgroundId, PilotBackgroundDefinition>
		{
			[EPilotBackgroundId.ScrapDrone] = new(
				EPilotBackgroundId.ScrapDrone,
				[
					EPilotCapabilityId.ScrapDroneSwarmDamageBonus,
					EPilotCapabilityId.MinorScrapRewardBonus,
				],
				new HudTooltipContent(
					"Scrap Drone",
					"It's a hard bot life, for us.\n Growing up in a factory is not a good life, most bots get scrapped themselves, those that make it out do it out of sheer grit.",
					$"[icon: ScrapDroneSwarm] - [icon: Damage, text: +{PilotSkillPercents.ScrapDrone.SwarmDamageBonusPercent}%]\n"
					+ "[icon: ScrapAlloy, text: +5%] gathered"),
				personalityModifier: new(2, 3, 2)),
			[EPilotBackgroundId.OptimalityEnforcer] = new(
				EPilotBackgroundId.OptimalityEnforcer,
				[EPilotCapabilityId.OptimalityContractCreditBonus],
				new HudTooltipContent(
					"Optimality Enforcer",
					"Some called him a simple thug, others called him a refined thug.\n Optimality enforcer school taught him one thing, if violence and zealotry don't work, you haven't applied enough of each.",
					"Contracts issued by The Optimality award [icon: Credits, text: +5%]."),
				personalityModifier: new(4, -2, -2)),
			[EPilotBackgroundId.CorporateSlave] = new(
				EPilotBackgroundId.CorporateSlave,
				[EPilotCapabilityId.InitiativePenalty],
				new HudTooltipContent(
					"Corporate Slave",
					"The cockpit or a cubicle. This bot will do what you ask.\n Corprate slaves that make it out are far and few between, but something in that upbringing stays with them forever.",
					"-10 Initiative."),
				personalityModifier: new(-3, 4, 3)),
			[EPilotBackgroundId.PodRacer] = new(
				EPilotBackgroundId.PodRacer,
				[EPilotCapabilityId.HeatGainReduction],
				new HudTooltipContent(
					"Pod Racer",
					"You win, or you're scrap. No in-between.",
					$"[icon: Heat, text: -{PilotSkillPercents.PodRacer.HeatGainReductionPercent}%] generation."),
				personalityModifier: new(0, -2, -2)),
			[EPilotBackgroundId.AsteroidSurfer] = new(
				EPilotBackgroundId.AsteroidSurfer,
				[
					EPilotCapabilityId.OutgoingDamageBonus,
					EPilotCapabilityId.IncreasedHullDamageTaken,
				],
				new HudTooltipContent(
					"Asteroid Surfer",
					"Surfing asteroids is dangerous. It's also very stupid.\n If you can believe it, theres actually an intergalactic scoreboard for this.",
					$"[icon: Damage, text: +{PilotSkillPercents.AsteroidSurfer.OutgoingDamageBonusPercent}%]\n"
					+ $"[icon: Hull, text: +{PilotSkillPercents.AsteroidSurfer.HullDamageTakenPercent - 100}%] damage taken."),
				personalityModifier: new(2, -4, -2)),
			[EPilotBackgroundId.SynthSinger] = new(
				EPilotBackgroundId.SynthSinger,
				[
					EPilotCapabilityId.HeatGainBonus,
					EPilotCapabilityId.HeatCapacityBonus,
				],
				new HudTooltipContent(
					"Synth Singer",
					"Singing has its highs, and lows.",
					$"[icon: Heat, text: +{PilotSkillPercents.SynthSinger.HeatGainBonusPercent}%] generation.\n"
					+ $"[icon: Heat, text: +{PilotSkillPercents.SynthSinger.HeatCapacityBonusPercent}%] max heat."),
				personalityModifier: new(-3, -5, -1)),
			[EPilotBackgroundId.RetiredMachineManager] = new(
				EPilotBackgroundId.RetiredMachineManager,
				[EPilotCapabilityId.SpawnedAllyDamageBonus],
				new HudTooltipContent(
					"Retired Machine Manager",
					"This bot enjoys control over others. Any divergence from protocol will get you punished. Severely.",
					$"Spawned units do [icon: Damage, text: +{PilotSkillPercents.RetiredMachineManager.SpawnedAllyDamageBonusPercent}%] damage."),
				personalityModifier: new(0, 2, 2)),
			[EPilotBackgroundId.CyberJunkie] = new(
				EPilotBackgroundId.CyberJunkie,
				[
					EPilotCapabilityId.ExperienceGainBonus,
					EPilotCapabilityId.HeatClearBonus,
				],
				new HudTooltipContent(
					"Cyber Junkie",
					"Cyber junkies always find the latest stupid tech to insert into their body, sometimes it works, sometimes it doesn't.",
					$"+{PilotSkillPercents.CyberJunkie.ExperienceGainBonusPercent}% XP\n"
					+ $"[icon: Heat, text: +{PilotSkillPercents.CyberJunkie.HeatClearBonusPercent}%] cleared / turn."),
				personalityModifier: new(0, -3, -1)),
			[EPilotBackgroundId.HydraulicAbuser] = new(
				EPilotBackgroundId.HydraulicAbuser,
				[EPilotCapabilityId.DamageResistance],
				new HudTooltipContent(
					"Hydraulic Abuser",
					"Some bots want to look STACKED, no idea why.",
					$"[icon: Hull, text: +{100 - PilotSkillPercents.HydraulicAbuser.DamageTakenPercent}%] damage resistance."),
				personalityModifier: new(3, 0, 0)),
			[EPilotBackgroundId.BeanCounter] = new(
				EPilotBackgroundId.BeanCounter,
				[
					EPilotCapabilityId.FleetSalaryDiscount,
					EPilotCapabilityId.BaseHullPenalty,
				],
				new HudTooltipContent(
					"Bean Counter",
					"All bots can do complex math, most of them just choose not to.",
					$"[icon: Salary, text: -{PilotSkillPercents.BeanCounter.FleetSalaryDiscountPercent}%] fleet salary.\n"
					+ $"[icon: Hull, text: -{100 - PilotSkillPercents.BeanCounter.BaseHullPercent}%] base hull."),
				personalityModifier: new(-2, 3, 0)),
			[EPilotBackgroundId.GearPsycho] = new(
				EPilotBackgroundId.GearPsycho,
				[EPilotCapabilityId.HazardPayDiscount],
				new HudTooltipContent(
					"Gear Psycho",
					"It always seems like the aggression circuit is tuned at 1000% with these bots.",
					$"[icon: HazardPay, text: -{100 - PilotSkillPercents.GearPsycho.HazardPayTakenPercent}%] hazard pay."),
				personalityModifier: new(3, 0, 0)),
		};

	public static PilotBackgroundDefinition Get(EPilotBackgroundId id) =>
		Definitions.TryGetValue(id, out var definition)
			? definition
			: throw new KeyNotFoundException($"Unknown pilot background '{id}'.");

	public static bool TryGet(EPilotBackgroundId id, out PilotBackgroundDefinition definition) =>
		Definitions.TryGetValue(id, out definition!);

	public static IEnumerable<PilotBackgroundDefinition> All => Definitions.Values;

	public static PilotBackgroundDefinition Roll(Random rng)
	{
		ArgumentNullException.ThrowIfNull(rng);
		var backgrounds = Definitions.Values.ToArray();
		return backgrounds[rng.Next(backgrounds.Length)];
	}
}
