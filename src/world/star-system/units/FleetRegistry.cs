using GrimSpace.World.StarSystem;

namespace GrimSpace.World.StarSystem.Units;

public sealed class FleetRegistry
{
	private readonly Dictionary<string, Fleet> _fleets = new(StringComparer.Ordinal);

	public static FleetRegistry For(StarMap world) => world.FleetRegistry;

	public IEnumerable<Fleet> All => _fleets.Values;

	public IEnumerable<string> Ids => _fleets.Keys;

	public Fleet FleetOf(string fleetId) => _fleets[fleetId];

	public bool TryGet(string fleetId, out Fleet fleet) => _fleets.TryGetValue(fleetId, out fleet!);

	public bool Contains(string fleetId) => _fleets.ContainsKey(fleetId);

	public void Add(Fleet fleet) =>
		_fleets[fleet.State.Id] = fleet;

	public bool Remove(string fleetId) => _fleets.Remove(fleetId);

	public void Replace(Fleet fleet) => _fleets[fleet.State.Id] = fleet;

	public bool TryFleetContainingMember(string memberId, out Fleet fleet)
	{
		foreach (var candidate in _fleets.Values)
		{
			if (!candidate.Members.Any(member => member.Id == memberId))
				continue;

			fleet = candidate;
			return true;
		}

		fleet = null!;
		return false;
	}

	public FleetRegistry CloneForFork()
	{
		var clone = new FleetRegistry();
		foreach (var fleet in _fleets.Values)
			clone.Add(CloneFleet(fleet));

		return clone;
	}

	private static Fleet CloneFleet(Fleet fleet) => new(fleet.State.Clone(), fleet.Members);
}
