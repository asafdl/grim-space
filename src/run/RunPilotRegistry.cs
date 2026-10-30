using GrimSpace.Pilots;
using GrimSpace.Run.Items;
using GrimSpace.Run.Persistence;
using GrimSpace.Units;

namespace GrimSpace.Run;

public sealed class RunPilotRegistry : IPilotSkillRosterReader
{
	/// <summary>Temporary flat roster: at most this many pilots, and all ship-assigned pilots are fielded.</summary>
	public const int MaxPilots = 4;
	public const int MaxActivePilots = MaxPilots;

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
			RegisterCore(Pilot.FromPersistence(dto));

		foreach (var dto in restored
			.Where(candidate => candidate.IsActive)
			.OrderBy(candidate => candidate.ActiveOrder))
		{
			if (!_pilotsById.ContainsKey(dto.Id))
				continue;

			_activePilotIds.Add(dto.Id);
		}
	}

	public bool CanRegister => _pilotsById.Count < MaxPilots;

	public void Register(Pilot pilot)
	{
		ArgumentNullException.ThrowIfNull(pilot);
		if (pilot.IsActive)
			throw new InvalidOperationException("A pilot must be activated through the roster.");
		if (!CanRegister)
			throw new InvalidOperationException($"Cannot register more than {MaxPilots} pilots.");

		RegisterCore(pilot);
	}

	private void RegisterCore(Pilot pilot)
	{
		if (_pilotsById.ContainsKey(pilot.Id))
			return;

		_pilotsById[pilot.Id] = pilot;
	}

	public void Update(Pilot pilot)
	{
		ArgumentNullException.ThrowIfNull(pilot);
		if (!_pilotsById.ContainsKey(pilot.Id))
			throw new KeyNotFoundException($"Pilot '{pilot.Id}' is not registered.");
		var current = _pilotsById[pilot.Id];
		if (pilot.IsActive != current.IsActive
			|| !string.Equals(
				pilot.AssignedShipId,
				current.AssignedShipId,
				StringComparison.Ordinal))
			throw new InvalidOperationException(
				"Pilot activation and ship assignment must use roster operations.");

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
		EnsureAssignmentCompatible(
			pilot,
			pilot.AssignedShipId,
			isActive: true,
			itemInventory,
			playerFleetUnitId);

		Replace(pilot.WithActive(true));
		_activePilotIds.Add(pilotId);
	}

	public void Deactivate(string pilotId)
	{
		ArgumentException.ThrowIfNullOrEmpty(pilotId);
		var pilot = GetRequired(pilotId);
		if (!pilot.IsActive)
			return;

		_activePilotIds.Remove(pilotId);
		Replace(pilot.WithActive(false));
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
		EnsureAssignmentCompatible(pilot, shipId, pilot.IsActive, itemInventory, playerFleetUnitId);
		Replace(pilot.WithAssignment(shipId, pilot.IsActive));
	}

	public void Unassign(string pilotId)
	{
		ArgumentException.ThrowIfNullOrEmpty(pilotId);
		var pilot = GetRequired(pilotId);
		if (pilot.IsActive)
			Deactivate(pilotId);

		Replace(pilot.WithAssignment(null, false));
	}

	public void SwapAssignments(
		string pilotIdA,
		string pilotIdB,
		Inventory itemInventory,
		string playerFleetUnitId)
	{
		ArgumentException.ThrowIfNullOrEmpty(pilotIdA);
		ArgumentException.ThrowIfNullOrEmpty(pilotIdB);
		ArgumentNullException.ThrowIfNull(itemInventory);
		ArgumentException.ThrowIfNullOrEmpty(playerFleetUnitId);
		var pilotA = GetRequired(pilotIdA);
		var pilotB = GetRequired(pilotIdB);
		var shipA = pilotA.AssignedShipId;
		var shipB = pilotB.AssignedShipId;
		EnsureAssignmentCompatible(pilotA, shipB, pilotA.IsActive, itemInventory, playerFleetUnitId);
		EnsureAssignmentCompatible(pilotB, shipA, pilotB.IsActive, itemInventory, playerFleetUnitId);
		Replace(pilotA.WithAssignment(shipB, pilotA.IsActive));
		Replace(pilotB.WithAssignment(shipA, pilotB.IsActive));
	}

	internal void AssignPilotToShip(
		string pilotId,
		string shipId,
		Inventory itemInventory,
		string playerFleetUnitId)
	{
		ArgumentException.ThrowIfNullOrEmpty(pilotId);
		ArgumentException.ThrowIfNullOrEmpty(shipId);
		ArgumentNullException.ThrowIfNull(itemInventory);
		ArgumentException.ThrowIfNullOrEmpty(playerFleetUnitId);
		var pilot = GetRequired(pilotId);
		var hasOccupant = TryGetForShip(shipId, out var occupant);
		if (hasOccupant && occupant.Id == pilotId)
		{
			EnsureAssignmentCompatible(
				pilot,
				shipId,
				pilot.IsActive,
				itemInventory,
				playerFleetUnitId);
			return;
		}

		var pilotActiveIndex = _activePilotIds.IndexOf(pilot.Id);
		var occupantActiveIndex = hasOccupant ? _activePilotIds.IndexOf(occupant.Id) : -1;
		var pilotShipId = pilot.AssignedShipId;
		// Occupied target: swap active slots with the occupant.
		// Empty target: always field (temporary: max pilots == max active, no hangar roster).
		var pilotIsActiveAfter = hasOccupant
			? occupantActiveIndex >= 0
			: true;
		var occupantIsActiveAfter = hasOccupant && pilotActiveIndex >= 0;

		if (!itemInventory.IsOwnedBy(shipId, playerFleetUnitId))
			throw new InvalidOperationException($"Ship '{shipId}' is not owned by '{playerFleetUnitId}'.");
		EnsureAssignmentCompatible(
			pilot,
			shipId,
			pilotIsActiveAfter,
			itemInventory,
			playerFleetUnitId);
		if (hasOccupant)
			EnsureAssignmentCompatible(
				occupant,
				pilotShipId,
				occupantIsActiveAfter,
				itemInventory,
				playerFleetUnitId);

		Replace(pilot.WithAssignment(shipId, pilotIsActiveAfter));
		if (hasOccupant)
			Replace(occupant.WithAssignment(pilotShipId, occupantIsActiveAfter));

		if (pilotActiveIndex >= 0 && hasOccupant)
			_activePilotIds[pilotActiveIndex] = occupant.Id;
		else if (!hasOccupant && pilotActiveIndex < 0 && pilotIsActiveAfter)
			_activePilotIds.Add(pilot.Id);

		if (occupantActiveIndex >= 0)
			_activePilotIds[occupantActiveIndex] = pilot.Id;
	}

	/// <summary>
	/// Fields assigned-but-inactive pilots into free active slots (heals hangar assignments
	/// that never activated, e.g. older saves).
	/// </summary>
	internal bool FillActiveSlotsFromAssigned(Inventory itemInventory, string playerFleetUnitId)
	{
		ArgumentNullException.ThrowIfNull(itemInventory);
		ArgumentException.ThrowIfNullOrEmpty(playerFleetUnitId);
		var changed = false;
		foreach (var pilot in _pilotsById.Values
			.Where(candidate => !candidate.IsActive && !string.IsNullOrEmpty(candidate.AssignedShipId))
			.OrderBy(candidate => candidate.Name, StringComparer.Ordinal)
			.ThenBy(candidate => candidate.Id, StringComparer.Ordinal)
			.ToArray())
		{
			if (_activePilotIds.Count >= MaxActivePilots)
				break;
			if (!itemInventory.IsOwnedBy(pilot.AssignedShipId!, playerFleetUnitId))
				continue;
			if (!OperatorClassCompatibility.CanOperate(
				pilot.ClassId,
				itemInventory.Get(pilot.AssignedShipId!).Spec.ClassId))
				continue;

			Activate(pilot.Id, itemInventory, playerFleetUnitId);
			changed = true;
		}

		return changed;
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

	public IReadOnlyList<PilotCapabilityGrant> QueryCapabilities(
		Func<EPilotCapabilityId, bool> capabilityMatches,
		Func<Pilot, bool>? pilotMatches = null)
	{
		ArgumentNullException.ThrowIfNull(capabilityMatches);
		var results = new List<PilotCapabilityGrant>();
		foreach (var pilot in _pilotsById.Values)
		{
			if (pilotMatches is not null && !pilotMatches(pilot))
				continue;

			results.AddRange(PilotCapabilityGrant.FromSkills(
				pilot.Id,
				pilot.LearnedSkills,
				capabilityMatches));
			results.AddRange(PilotCapabilityGrant.FromBackground(
				pilot.Id,
				pilot.BackgroundId,
				capabilityMatches));
			var quirkIds = new List<EPilotQuirkId>(2);
			if (pilot.PositiveQuirkId is { } positiveQuirkId)
				quirkIds.Add(positiveQuirkId);
			if (pilot.NegativeQuirkId is { } negativeQuirkId)
				quirkIds.Add(negativeQuirkId);
			if (quirkIds.Count > 0)
				results.AddRange(PilotCapabilityGrant.FromQuirks(
					pilot.Id,
					quirkIds,
					capabilityMatches));
		}

		return results;
	}

	public IReadOnlyList<PilotCapabilityGrant> QueryCapabilitiesForActive(
		Func<EPilotCapabilityId, bool> capabilityMatches) =>
		QueryCapabilities(capabilityMatches, pilot => pilot.IsActive);

	public IReadOnlyList<PilotCapabilityGrant> QueryCapabilitiesForShip(
		string shipId,
		Func<EPilotCapabilityId, bool> capabilityMatches)
	{
		ArgumentException.ThrowIfNullOrEmpty(shipId);
		ArgumentNullException.ThrowIfNull(capabilityMatches);
		if (!TryGetForShip(shipId, out var pilot))
			return [];

		return QueryCapabilities(
			capabilityMatches,
			candidate => string.Equals(candidate.Id, pilot.Id, StringComparison.Ordinal));
	}

	private Pilot GetRequired(string pilotId) =>
		TryGet(pilotId, out var pilot)
			? pilot
			: throw new KeyNotFoundException($"Pilot '{pilotId}' is not registered.");

	private void Replace(Pilot pilot) =>
		_pilotsById[pilot.Id] = pilot;

	private static void EnsureAssignmentCompatible(
		Pilot pilot,
		string? shipId,
		bool isActive,
		Inventory itemInventory,
		string playerFleetUnitId)
	{
		if (string.IsNullOrEmpty(shipId))
		{
			if (isActive)
				throw new InvalidOperationException($"Active pilot '{pilot.Id}' must have an assigned ship.");
			return;
		}

		if (!itemInventory.IsOwnedBy(shipId, playerFleetUnitId))
			throw new InvalidOperationException(
				$"Pilot '{pilot.Id}' is assigned to ship '{shipId}' that is not player-owned.");

		var shipClass = itemInventory.Get(shipId).Spec.ClassId;
		if (!OperatorClassCompatibility.CanOperate(pilot.ClassId, shipClass))
			throw new InvalidOperationException(
				$"Pilot '{pilot.Id}' with class '{pilot.ClassId}' cannot operate ship '{shipId}' with class '{shipClass}'.");
	}
}
