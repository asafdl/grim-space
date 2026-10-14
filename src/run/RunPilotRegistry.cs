using GrimSpace.Pilots;
using GrimSpace.Run.Items;
using GrimSpace.Run.Persistence;

namespace GrimSpace.Run;

public sealed class RunPilotRegistry : IPilotSkillRosterReader
{
	public const int MaxActivePilots = 4;

	private readonly Dictionary<string, Pilot> _pilotsById = new(StringComparer.Ordinal);
	private readonly List<string> _activePilotIds = [];

	public IReadOnlyCollection<Pilot> All => _pilotsById.Values;

	public IReadOnlyList<Pilot> Active =>
		_activePilotIds
			.Select(id => _pilotsById[id])
			.ToArray();

	internal IReadOnlyList<PilotPersistenceDto> CaptureSnapshot()
	{
		var activeOrder = _activePilotIds
			.Select((id, index) => (id, index))
			.ToDictionary(pair => pair.id, pair => pair.index, StringComparer.Ordinal);
		return _pilotsById.Values
			.Select(pilot => pilot.ToPersistence(
				activeOrder.TryGetValue(pilot.Id, out var order) ? order : -1))
			.ToArray();
	}

	internal void RestoreSnapshot(IEnumerable<PilotPersistenceDto> pilots)
	{
		_pilotsById.Clear();
		_activePilotIds.Clear();
		var restored = pilots.ToArray();
		foreach (var dto in restored)
			Register(Pilot.FromPersistence(dto));

		foreach (var dto in restored
			.Where(candidate => candidate.IsActive)
			.OrderBy(candidate => candidate.ActiveOrder))
		{
			if (!_pilotsById.ContainsKey(dto.Id))
				continue;

			_activePilotIds.Add(dto.Id);
		}
	}

	internal void RestoreActiveFromLegacyPartyShipIds(IReadOnlyList<string> partyShipIds)
	{
		if (_activePilotIds.Count > 0 || partyShipIds.Count == 0)
			return;

		foreach (var shipId in partyShipIds)
		{
			if (!TryGetForShip(shipId, out var pilot))
				throw new InvalidDataException($"Party ship '{shipId}' has no assigned pilot.");

			if (_activePilotIds.Count >= MaxActivePilots)
				break;

			Update(pilot.WithActive(true));
			_activePilotIds.Add(pilot.Id);
		}
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

	public void Activate(string pilotId, Inventory itemInventory, string playerFleetUnitId)
	{
		ArgumentException.ThrowIfNullOrEmpty(pilotId);
		ArgumentNullException.ThrowIfNull(itemInventory);
		ArgumentException.ThrowIfNullOrEmpty(playerFleetUnitId);
		var pilot = GetRequired(pilotId);
		if (pilot.IsActive)
			return;
		if (_activePilotIds.Count >= MaxActivePilots)
			throw new InvalidOperationException($"Cannot activate more than {MaxActivePilots} pilots.");

		if (string.IsNullOrEmpty(pilot.AssignedShipId))
			throw new InvalidOperationException($"Pilot '{pilotId}' has no assigned ship.");

		if (!itemInventory.IsOwnedBy(pilot.AssignedShipId, playerFleetUnitId))
			throw new InvalidOperationException(
				$"Pilot '{pilotId}' is assigned to ship '{pilot.AssignedShipId}' that is not player-owned.");

		Update(pilot.WithActive(true));
		_activePilotIds.Add(pilotId);
	}

	public void Deactivate(string pilotId)
	{
		ArgumentException.ThrowIfNullOrEmpty(pilotId);
		var pilot = GetRequired(pilotId);
		if (!pilot.IsActive)
			return;

		_activePilotIds.Remove(pilotId);
		Update(pilot.WithActive(false));
	}

	public void AssignShip(
		string pilotId,
		string shipId,
		Inventory itemInventory,
		string playerFleetUnitId)
	{
		ArgumentException.ThrowIfNullOrEmpty(pilotId);
		ArgumentException.ThrowIfNullOrEmpty(shipId);
		ArgumentNullException.ThrowIfNull(itemInventory);
		ArgumentException.ThrowIfNullOrEmpty(playerFleetUnitId);
		if (!itemInventory.IsOwnedBy(shipId, playerFleetUnitId))
			throw new InvalidOperationException($"Ship '{shipId}' is not owned by '{playerFleetUnitId}'.");

		if (TryGetForShip(shipId, out var occupant) && occupant.Id != pilotId)
			throw new InvalidOperationException($"Ship '{shipId}' is already assigned to pilot '{occupant.Id}'.");

		var pilot = GetRequired(pilotId);
		Update(pilot.WithAssignment(shipId, pilot.IsActive));
	}

	public void Unassign(string pilotId)
	{
		ArgumentException.ThrowIfNullOrEmpty(pilotId);
		var pilot = GetRequired(pilotId);
		if (pilot.IsActive)
			Deactivate(pilotId);

		Update(pilot.WithAssignment(null, false));
	}

	public void SwapAssignments(string pilotIdA, string pilotIdB)
	{
		ArgumentException.ThrowIfNullOrEmpty(pilotIdA);
		ArgumentException.ThrowIfNullOrEmpty(pilotIdB);
		var pilotA = GetRequired(pilotIdA);
		var pilotB = GetRequired(pilotIdB);
		var shipA = pilotA.AssignedShipId;
		var shipB = pilotB.AssignedShipId;
		Update(pilotA.WithAssignment(shipB, pilotA.IsActive));
		Update(pilotB.WithAssignment(shipA, pilotB.IsActive));
	}

	public void Remove(string pilotId)
	{
		ArgumentException.ThrowIfNullOrEmpty(pilotId);
		_activePilotIds.Remove(pilotId);
		_pilotsById.Remove(pilotId);
	}

	public void RemoveForShip(string shipId)
	{
		ArgumentException.ThrowIfNullOrEmpty(shipId);
		foreach (var pilot in _pilotsById.Values)
		{
			if (!string.Equals(pilot.AssignedShipId, shipId, StringComparison.Ordinal))
				continue;

			_activePilotIds.Remove(pilot.Id);
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

	public IReadOnlyList<PilotSkillGrant> QueryLearnedForActive(
		Func<EPilotSkillTag, bool> tagMatches) =>
		QueryLearned(tagMatches, pilot => pilot.IsActive);

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

	private Pilot GetRequired(string pilotId) =>
		TryGet(pilotId, out var pilot)
			? pilot
			: throw new KeyNotFoundException($"Pilot '{pilotId}' is not registered.");
}
