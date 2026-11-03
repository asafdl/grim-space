namespace GrimSpace.Units.Loadouts.Abilities;

public static class AbilityReach
{
	/// <summary>Optimistic manhattan distance from firer origin to farthest affected cell.</summary>
	public static int MaxManhattanFromFirer(AbilitySpec spec) =>
		spec switch
		{
			ScrapDroneSwarmSpec swarm => swarm.BurstRange * 3 + 1,
			LightningCannonSpec lightningCannon =>
				lightningCannon.LineLength + lightningCannon.MaxChains * lightningCannon.ChainRange,
			RamSpec ram => ram.Range,
			HighSpeedRodSpec rod => rod.Range,
			RoadsWeDontNeedRoadsSpec jump => jump.Range,
			_ => 0,
		};

	public static float ScrapDroneSwarmReplayShotLength(ScrapDroneSwarmSpec spec) =>
		spec.BurstRange + 1.6f;

	public static float LightningCannonReplayShotLength(LightningCannonSpec spec) =>
		spec.LineLength + spec.MaxChains * spec.ChainRange + 0.7f;
}
