namespace GrimSpace.World.StarSystem.Resources;

public sealed class ResourceBundle : IEnumerable<KeyValuePair<ResourceId, int>>
{
	public static ResourceBundle Empty { get; } = new([]);

	private readonly Dictionary<ResourceId, int> _entries;

	private ResourceBundle(Dictionary<ResourceId, int> entries) => _entries = entries;

	public bool IsEmpty => _entries.Count == 0;

	public static ResourceBundle Of(ResourceId id, int amount) =>
		amount == 0 ? Empty : Create((id, amount));

	public static ResourceBundle Create(params ReadOnlySpan<(ResourceId Id, int Amount)> entries)
	{
		var normalized = new Dictionary<ResourceId, int>();
		foreach (var (id, amount) in entries)
		{
			if (amount == 0)
				continue;

			if (normalized.TryGetValue(id, out var existing))
				normalized[id] = existing + amount;
			else
				normalized[id] = amount;
		}

		return normalized.Count == 0 ? Empty : new ResourceBundle(normalized);
	}

	public static ResourceBundle Create(IReadOnlyDictionary<ResourceId, int> entries)
	{
		ArgumentNullException.ThrowIfNull(entries);

		var normalized = new Dictionary<ResourceId, int>();
		foreach (var (id, amount) in entries)
		{
			if (amount == 0)
				continue;

			if (normalized.TryGetValue(id, out var existing))
				normalized[id] = existing + amount;
			else
				normalized[id] = amount;
		}

		return normalized.Count == 0 ? Empty : new ResourceBundle(normalized);
	}

	public bool TryGet(ResourceId id, out int amount) => _entries.TryGetValue(id, out amount);

	public ResourceBundle Negate()
	{
		if (IsEmpty)
			return Empty;

		var inverted = new Dictionary<ResourceId, int>(_entries.Count);
		foreach (var (id, amount) in _entries)
			inverted[id] = -amount;

		return new ResourceBundle(inverted);
	}

	public IEnumerator<KeyValuePair<ResourceId, int>> GetEnumerator() => _entries.GetEnumerator();

	System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();

	public override bool Equals(object? obj) => obj is ResourceBundle other && Equals(other);

	public bool Equals(ResourceBundle? other)
	{
		if (other is null)
			return false;

		if (_entries.Count != other._entries.Count)
			return false;

		foreach (var (id, amount) in _entries)
		{
			if (!other.TryGet(id, out var otherAmount) || otherAmount != amount)
				return false;
		}

		return true;
	}

	public override int GetHashCode()
	{
		var hash = new HashCode();
		foreach (ResourceId id in Enum.GetValues<ResourceId>())
		{
			if (!_entries.TryGetValue(id, out var amount))
				continue;

			hash.Add(id);
			hash.Add(amount);
		}

		return hash.ToHashCode();
	}
}
