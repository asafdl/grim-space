using GrimSpace.Pilots;
using GrimSpace.Run.Persistence;

namespace GrimSpace.Run;

public sealed class RunPilotRegistry : IPilotSkillRosterReader
{
	private readonly Dictionary<string, Pilot> _pilotsById = new(StringComparer.Ordinal);

	public IReadOnlyCollection<Pilot> All => _pilotsById.Values;

	internal IReadOnlyList<PilotPersistenceDto> CaptureSnapshot() =>
		_pilotsById.Values.Select(pilot => pilot.ToPersistence()).ToArray();

	internal void RestoreSnapshot(IEnumerable<PilotPersistenceDto> pilots)
	{
		_pilotsById.Clear();
		foreach (var dto in pilots)
			Register(Pilot.FromPersistence(dto));
	}

	public void Register(Pilot pilot)
	{
		ArgumentNullException.ThrowIfNull(pilot);
		if (_pilotsById.ContainsKey(pilot.Id))
			return;

		_pilotsById[pilot.Id] = pilot;
	}

	public void Update(Pilot pilot)
	{
		ArgumentNullException.ThrowIfNull(pilot);
		if (!_pilotsById.ContainsKey(pilot.Id))
			throw new KeyNotFoundException($"Pilot '{pilot.Id}' is not registered.");

		_pilotsById[pilot.Id] = pilot;
	}

	public void Remove(string pilotId)
	{
		ArgumentException.ThrowIfNullOrEmpty(pilotId);
		_pilotsById.Remove(pilotId);
	}

	public void RemoveForShip(string shipId)
	{
		ArgumentException.ThrowIfNullOrEmpty(shipId);
		foreach (var pilot in _pilotsById.Values)
		{
			if (!string.Equals(pilot.AssignedShipId, shipId, StringComparison.Ordinal))
				continue;

			_pilotsById.Remove(pilot.Id);
			return;
		}
	}

	public bool TryGet(string pilotId, out Pilot pilot) =>
		_pilotsById.TryGetValue(pilotId, out pilot!);

	public int LearnedCount(EPilotSkillId skillId) =>
		_pilotsById.Values.Count(pilot => pilot.LearnedSkills.Contains(skillId));

	public bool TryGetForShip(string shipId, out Pilot pilot)
	{
		ArgumentException.ThrowIfNullOrEmpty(shipId);
		foreach (var candidate in _pilotsById.Values)
		{
			if (string.Equals(candidate.AssignedShipId, shipId, StringComparison.Ordinal))
			{
				pilot = candidate;
				return true;
			}
		}

		pilot = null!;
		return false;
	}
}
