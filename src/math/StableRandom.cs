namespace GrimSpace.Math;

public struct StableRandom
{
	private ulong _state;

	public StableRandom(ulong seed) => _state = seed;

	public double NextDouble() =>
		(NextUInt64() >> 11) * (1.0 / (1UL << 53));

	private ulong NextUInt64()
	{
		_state += 0x9E3779B97F4A7C15UL;
		var value = _state;
		value = (value ^ (value >> 30)) * 0xBF58476D1CE4E5B9UL;
		value = (value ^ (value >> 27)) * 0x94D049BB133111EBUL;
		return value ^ (value >> 31);
	}
}

public sealed class StableSeedMixer
{
	private const ulong Offset = 14695981039346656037UL;
	private const ulong Prime = 1099511628211UL;

	private ulong _hash = Offset;

	public static StableSeedMixer From(int seed) => new StableSeedMixer().Add(seed);

	public StableSeedMixer Add(int value)
	{
		for (var shift = 0; shift < 32; shift += 8)
		{
			_hash ^= (byte)(value >> shift);
			_hash *= Prime;
		}

		return this;
	}

	public StableSeedMixer Add(string value)
	{
		foreach (var character in value)
		{
			_hash ^= (byte)character;
			_hash *= Prime;
			_hash ^= (byte)(character >> 8);
			_hash *= Prime;
		}

		return this;
	}

	public ulong Value => _hash;
}
