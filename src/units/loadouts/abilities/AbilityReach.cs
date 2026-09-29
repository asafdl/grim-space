namespace GrimSpace.Units.Loadouts.Abilities;

public static class AbilityReach
{
	/// <summary>Optimistic manhattan distance from firer origin to farthest affected cell.</summary>
	public static int MaxManhattanFromFirer(AbilitySpec spec) =>
		spec switch
		{
			ScrapDroneSwarmSpec swarm => swarm.BurstRange * 3 + 1,
			LightningCannonSpec lightningCannon => lightningCannon.LineLength + 2 * lightningCannon.PyramidRange,
			_ => 0,
		};

	public static float ScrapDroneSwarmReplayShotLength(ScrapDroneSwarmSpec spec) =>
		spec.BurstRange + 1.6f;

	public static float LightningCannonReplayShotLength(LightningCannonSpec spec) =>
		spec.LineLength + spec.PyramidRange + 0.7f;
}
