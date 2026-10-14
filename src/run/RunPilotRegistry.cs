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

	public IReadOnlyList<PilotSkillGrant> QueryLearned(
		Func<EPilotSkillTag, bool> tagMatches,
		Func<Pilot, bool>? pilotMatches = null)
	{
		ArgumentNullException.ThrowIfNull(tagMatches);
		var results = new List<PilotSkillGrant>();
		foreach (var pilot in _pilotsById.Values)
		{
			if (pilotMatches is not null && !pilotMatches(pilot))
				continue;

			foreach (var skillId in pilot.LearnedSkills)
			{
				if (!tagMatches(PilotSkillCatalog.Get(skillId).Tag))
					continue;

				results.Add(new PilotSkillGrant(skillId, pilot.Id));
			}
		}

		return results;
	}

	public IReadOnlyList<PilotSkillGrant> QueryLearnedForParty(
		Func<EPilotSkillTag, bool> tagMatches,
		IReadOnlyList<string> partyShipIds)
	{
		ArgumentNullException.ThrowIfNull(partyShipIds);
		if (partyShipIds.Count == 0)
			return [];

		var partyShips = partyShipIds.ToHashSet(StringComparer.Ordinal);
		return QueryLearned(tagMatches, pilot => partyShips.Contains(pilot.AssignedShipId));
	}

	public IReadOnlyList<PilotSkillGrant> QueryLearnedForShip(
		string shipId,
		Func<EPilotSkillTag, bool> tagMatches)
	{
		ArgumentException.ThrowIfNullOrEmpty(shipId);
		ArgumentNullException.ThrowIfNull(tagMatches);
		if (!TryGetForShip(shipId, out var pilot))
			return [];

		return QueryLearned(tagMatches, candidate => string.Equals(candidate.Id, pilot.Id, StringComparison.Ordinal));
	}

	public static bool IsStrategicPassiveTag(EPilotSkillTag tag) =>
		tag == EPilotSkillTag.StrategicPassive;

	public static bool IsBattleTag(EPilotSkillTag tag) =>
		tag is EPilotSkillTag.BattleAbility or EPilotSkillTag.BattlePassive;
}
