namespace GrimSpace.Units.Loadouts.Abilities;

public static class AbilityReach
{
	/// <summary>Optimistic manhattan distance from firer origin to farthest affected cell.</summary>
	public static int MaxManhattanFromFirer(AbilitySpec spec) =>
		spec switch
		{
			FlakSpec flak => flak.BurstRange * 3 + 1,
			RailgunSpec railgun => railgun.LineLength + 2 * railgun.PyramidRange,
			_ => 0,
		};

	public static float FlakReplayShotLength(FlakSpec spec) =>
		spec.BurstRange + 1.6f;

	public static float RailgunReplayShotLength(RailgunSpec spec) =>
		spec.LineLength + spec.PyramidRange + 0.7f;
}
