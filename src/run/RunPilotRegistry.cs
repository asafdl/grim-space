using GrimSpace.Pilots;
using GrimSpace.Run.Items;
using GrimSpace.Run.Persistence;
using GrimSpace.Units;

namespace GrimSpace.Run;

public sealed class RunPilotRegistry : IPilotSkillRosterReader
{
	public const int MaxPilots = 4;
	public const int MaxDeployedPilots = 4;

	private readonly Dictionary<string, Pilot> _pilotsById = new(StringComparer.Ordinal);
	private readonly List<string> _lastDeployment = [];

	public event Action? PilotRemoved;

	public IReadOnlyCollection<Pilot> All => _pilotsById.Values;

	/// <summary>Pilots fielded in the most recent engagement, in slot order.</summary>
	public IReadOnlyList<string> LastDeployment => _lastDeployment;

	internal void RecordDeployment(IEnumerable<string> pilotIds)
	{
		_lastDeployment.Clear();
		_lastDeployment.AddRange(pilotIds);
	}

	/// <summary>Pilots with an assigned ship; only these can be deployed.</summary>
	public IReadOnlyList<Pilot> Assigned =>
		_pilotsById.Values.Where(pilot => !string.IsNullOrEmpty(pilot.AssignedShipId)).ToArray();

	internal IReadOnlyList<PilotPersistenceDto> CaptureSnapshot() =>
		_pilotsById.Values
			.Select(pilot => pilot.ToPersistence(_lastDeployment.IndexOf(pilot.Id)))
			.ToArray();

	internal void RestoreSnapshot(IEnumerable<PilotPersistenceDto> pilots)
	{
		_pilotsById.Clear();
		_lastDeployment.Clear();
		var restored = pilots.ToArray();
		foreach (var dto in restored)
			RegisterCore(Pilot.FromPersistence(dto));

		_lastDeployment.AddRange(restored
			.Where(dto => dto.DeploymentOrder >= 0)
			.OrderBy(dto => dto.DeploymentOrder)
			.Select(dto => dto.Id));
	}

	public bool CanRegister => _pilotsById.Count < MaxPilots;

	public void Register(Pilot pilot)
	{
		ArgumentNullException.ThrowIfNull(pilot);
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
		if (!string.Equals(pilot.AssignedShipId, current.AssignedShipId, StringComparison.Ordinal))
			throw new InvalidOperationException("Ship assignment must use roster operations.");

		_pilotsById[pilot.Id] = pilot;
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
		EnsureAssignmentCompatible(pilot, shipId, itemInventory, playerFleetUnitId);
		Replace(pilot.WithAssignment(shipId));
	}

	public void Unassign(string pilotId)
	{
		ArgumentException.ThrowIfNullOrEmpty(pilotId);
		Replace(GetRequired(pilotId).WithAssignment(null));
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
		EnsureAssignmentCompatible(pilotA, shipB, itemInventory, playerFleetUnitId);
		EnsureAssignmentCompatible(pilotB, shipA, itemInventory, playerFleetUnitId);
		Replace(pilotA.WithAssignment(shipB));
		Replace(pilotB.WithAssignment(shipA));
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
		if (!itemInventory.IsOwnedBy(shipId, playerFleetUnitId))
			throw new InvalidOperationException($"Ship '{shipId}' is not owned by '{playerFleetUnitId}'.");
		EnsureAssignmentCompatible(pilot, shipId, itemInventory, playerFleetUnitId);
		if (hasOccupant && occupant.Id == pilotId)
			return;

		// An occupied target swaps ships with its occupant.
		if (hasOccupant)
		{
			EnsureAssignmentCompatible(occupant, pilot.AssignedShipId, itemInventory, playerFleetUnitId);
			Replace(occupant.WithAssignment(pilot.AssignedShipId));
		}

		Replace(pilot.WithAssignment(shipId));
	}

	public void Remove(string pilotId)
	{
		ArgumentException.ThrowIfNullOrEmpty(pilotId);
		_lastDeployment.Remove(pilotId);
		if (!_pilotsById.Remove(pilotId))
			return;

		PilotRemoved?.Invoke();
	}

	public void RemoveForShip(string shipId)
	{
		ArgumentException.ThrowIfNullOrEmpty(shipId);
		foreach (var pilot in _pilotsById.Values)
		{
			if (!string.Equals(pilot.AssignedShipId, shipId, StringComparison.Ordinal))
				continue;

			_lastDeployment.Remove(pilot.Id);
			_pilotsById.Remove(pilot.Id);
			PilotRemoved?.Invoke();
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
			results.AddRange(PilotCapabilityGrant.FromQuirks(
				pilot.Id,
				pilot.PositiveQuirkIds.Concat(pilot.NegativeQuirkIds),
				capabilityMatches));
		}

		return results;
	}

	public IReadOnlyList<PilotCapabilityGrant> QueryCapabilitiesForAssigned(
		Func<EPilotCapabilityId, bool> capabilityMatches) =>
		QueryCapabilities(capabilityMatches, pilot => !string.IsNullOrEmpty(pilot.AssignedShipId));

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
		Inventory itemInventory,
		string playerFleetUnitId)
	{
		if (string.IsNullOrEmpty(shipId))
			return;

		if (!itemInventory.IsOwnedBy(shipId, playerFleetUnitId))
			throw new InvalidOperationException(
				$"Pilot '{pilot.Id}' is assigned to ship '{shipId}' that is not player-owned.");

		var shipClass = itemInventory.Get(shipId).Spec.ClassId;
		if (!OperatorClassCompatibility.CanOperate(pilot.ClassId, shipClass))
			throw new InvalidOperationException(
				$"Pilot '{pilot.Id}' with class '{pilot.ClassId}' cannot operate ship '{shipId}' with class '{shipClass}'.");
	}
}
