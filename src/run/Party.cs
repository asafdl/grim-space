// Placeholder until roguelike sector map exists.

namespace GrimSpace.Run;

public sealed class Party
{
	private readonly List<string> _shipIds = [];

	public IReadOnlyList<string> ShipIds => _shipIds;

	internal void Add(string shipId) => _shipIds.Add(shipId);

	internal void Remove(string shipId) => _shipIds.Remove(shipId);

	internal IReadOnlyList<string> CaptureSnapshot() => _shipIds.ToArray();

	internal void RestoreSnapshot(IEnumerable<string> shipIds)
	{
		_shipIds.Clear();
		_shipIds.AddRange(shipIds);
	}
}
