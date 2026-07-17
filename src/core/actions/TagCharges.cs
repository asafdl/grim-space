namespace GrimSpace.Core.Actions;

/// <summary>
/// String-keyed integer charges — building block inside typed turn-state objects.
/// </summary>
public sealed class TagCharges
{
	private readonly Dictionary<string, int> _charges = new();

	public void Add(string key, int amount) => Set(key, Get(key) + amount);

	public int Get(string key) => _charges.TryGetValue(key, out var value) ? value : 0;

	public void Set(string key, int amount)
	{
		if (amount == 0)
			_charges.Remove(key);
		else
			_charges[key] = amount;
	}

	public bool TryConsume(string key, int amount)
	{
		if (Get(key) < amount)
			return false;

		Set(key, Get(key) - amount);
		return true;
	}

	public void Clear() => _charges.Clear();

	public int GetNormalized(string key, int mod) => ((Get(key) % mod) + mod) % mod;
}
