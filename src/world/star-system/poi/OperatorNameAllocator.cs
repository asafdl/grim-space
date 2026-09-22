using GrimSpace.Math;

namespace GrimSpace.World.StarSystem.Poi;

public sealed class OperatorNameAllocator
{
	private readonly List<string> _remaining;

	public OperatorNameAllocator(int mapSeed)
	{
		var random = new StableRandom(StableSeedMixer.From(mapSeed).Add("operator-names").Value);
		_remaining = OperatorNames.Pool.ToList();
		Shuffle(_remaining, random);
	}

	public string Take()
	{
		if (_remaining.Count == 0)
			throw new InvalidOperationException("Operator name pool exhausted for this map.");

		var name = _remaining[^1];
		_remaining.RemoveAt(_remaining.Count - 1);
		return name;
	}

	private static void Shuffle(List<string> items, StableRandom random)
	{
		for (var i = items.Count - 1; i > 0; i--)
		{
			var j = (int)(random.NextDouble() * (i + 1));
			(items[i], items[j]) = (items[j], items[i]);
		}
	}
}
