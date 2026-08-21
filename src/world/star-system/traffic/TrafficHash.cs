namespace GrimSpace.World.StarSystem.Traffic;

internal static class TrafficHash
{
	public static int Mix(int seed, string a, string b) =>
		Mix(seed, a, b, string.Empty, string.Empty);

	public static int Mix(int seed, string a, string b, string c) =>
		Mix(seed, a, b, c, string.Empty);

	public static int Mix(int seed, string a, string b, string c, string d)
	{
		var hash = seed;
		hash = Mix(hash, a);
		hash = Mix(hash, b);
		hash = Mix(hash, c);
		hash = Mix(hash, d);
		return hash;
	}

	public static int Mix(int seed, string value)
	{
		var hash = seed;
		foreach (var ch in value)
			hash = unchecked(hash * 31 + ch);
		return hash;
	}

	public static double Unit(int hash) =>
		(hash & 0x7fffffff) / (double)0x7fffffff;

	public static int Range(int hash, int minInclusive, int maxInclusive) =>
		minInclusive + (hash & 0x7fffffff) % (maxInclusive - minInclusive + 1);
}
