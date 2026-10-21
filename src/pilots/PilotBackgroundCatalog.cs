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
				"Scrap Drone",
				"It's a hard bot life, for us.\n"
				+ "Scrap Drone Swarm deals +1 damage.\nScrap Alloy gathered increased by 5%."),
			[EPilotBackgroundId.OptimalityEnforcer] = new(
				EPilotBackgroundId.OptimalityEnforcer,
				[EPilotCapabilityId.OptimalityContractCreditBonus],
				"Optimality Enforcer",
				"Some called him a simple thug, others called him a refined thug.\n"
				+ "Contracts issued by The Optimality award 5% more Credits.\n"
				+ "Lowers team satisfaction (TBD)."),
			[EPilotBackgroundId.CorporateSlave] = new(
				EPilotBackgroundId.CorporateSlave,
				[EPilotCapabilityId.InitiativePenalty],
				"Corporate Slave",
				"The cockpit or a cubicle. This bot will do what you ask.\n"
				+ "Assigned ship has -10 Initiative."),
			[EPilotBackgroundId.PodRacer] = new(
				EPilotBackgroundId.PodRacer,
				[],
				"Pod Racer",
				"You win, or you're scrap. No in-between.\n"
				+ "+1 Maneuverability on small ships (TBD)."),
			[EPilotBackgroundId.AsteroidSurfer] = new(
				EPilotBackgroundId.AsteroidSurfer,
				[],
				"Asteroid Surfer",
				"Surfing asteroids is dangerous. It's also very stupid.\n"
				+ "Takes 15% more damage and deals 10% more damage (TBD)."),
			[EPilotBackgroundId.SynthSinger] = new(
				EPilotBackgroundId.SynthSinger,
				[],
				"Synth Singer",
				"Singing has its highs, and lows.\n"
				+ "Prone to addiction quirks (TBD)."),
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
