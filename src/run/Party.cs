// Placeholder until roguelike sector map exists.

namespace GrimSpace.Run;

public sealed class Party
{
	private readonly List<string> _shipIds = [];

	public IReadOnlyList<string> ShipIds => _shipIds;

	public void Add(string shipId) => _shipIds.Add(shipId);

	public void Remove(string shipId) => _shipIds.Remove(shipId);
}
