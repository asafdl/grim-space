using GrimSpace.Battle.Objectives;
using GrimSpace.Units;
using GrimSpace.Units.Enums;
using GrimSpace.Units.Loadouts.Abilities;
using GrimSpace.World.StarSystem.Ids;

namespace GrimSpace.Run.Items;

public sealed class Inventory
{
	private readonly Dictionary<string, ItemEntry> _entries = new(StringComparer.Ordinal);

	public IReadOnlyCollection<ShipInstance> All =>
		_entries.Values.Select(entry => AsShip(entry.Item)).ToArray();

	internal IReadOnlyList<InventorySnapshotEntry> CaptureSnapshot() =>
		_entries.Values
			.Select(entry =>
			{
				var ship = AsShip(entry.Item);
				return new InventorySnapshotEntry(ship.Clone(), entry.OwnerId);
			})
			.ToArray();

	internal void RestoreSnapshot(IEnumerable<InventorySnapshotEntry> entries)
	{
		_entries.Clear();
		foreach (var entry in entries)
			Register(entry.Ship, entry.OwnerId);
	}

	public void Register(IItem item, string ownerId)
	{
		ArgumentNullException.ThrowIfNull(item);
		ArgumentException.ThrowIfNullOrEmpty(ownerId);
		var ship = AsShip(item);
		if (_entries.TryGetValue(ship.Id, out var existing))
		{
			var existingShip = AsShip(existing.Item);
			if (existingShip.Spec.Chassis != ship.Spec.Chassis)
			{
				throw new InvalidOperationException(
					$"Item '{ship.Id}' is already registered with chassis '{existingShip.Spec.Chassis}'; "
					+ $"cannot register '{ship.Spec.Chassis}'.");
			}

			if (!string.Equals(existing.OwnerId, ownerId, StringComparison.Ordinal))
			{
				throw new InvalidOperationException(
					$"Item '{ship.Id}' is already registered to owner '{existing.OwnerId}'; "
					+ $"cannot register to '{ownerId}'.");
			}

			return;
		}

		_entries[ship.Id] = new ItemEntry(ship, ownerId);
	}

	public void Register(ShipInstance ship, string ownerId) => Register((IItem)ship, ownerId);

	public void Update(ShipInstance ship)
	{
		ArgumentNullException.ThrowIfNull(ship);
		if (!_entries.ContainsKey(ship.Id))
			throw new KeyNotFoundException($"Ship '{ship.Id}' is not registered.");

		var ownerId = _entries[ship.Id].OwnerId;
		_entries[ship.Id] = new ItemEntry(ship, ownerId);
	}

	public void Remove(string itemId)
	{
		ArgumentException.ThrowIfNullOrEmpty(itemId);
		if (!_entries.Remove(itemId))
			throw new KeyNotFoundException($"Ship '{itemId}' is not registered.");
	}

	internal void ReassignOwner(string itemId, string ownerId)
	{
		ArgumentException.ThrowIfNullOrEmpty(itemId);
		ArgumentException.ThrowIfNullOrEmpty(ownerId);
		if (!_entries.TryGetValue(itemId, out var entry))
			throw new KeyNotFoundException($"Ship '{itemId}' is not registered.");

		_entries[itemId] = new ItemEntry(AsShip(entry.Item), ownerId);
	}

	public void Register(ShipSpawnDeclaration declaration, string ownerId) =>
		Register(ShipInstance.FromSpec(
			declaration.ShipId,
			ShipCatalog.SpecFor(declaration.Chassis),
			ShipCatalog.LoadoutForTier(declaration.Chassis, declaration.GearTier, declaration.RollSeed)),
			ownerId);

	public bool TryGetOwnerId(string itemId, out string ownerId)
	{
		if (_entries.TryGetValue(itemId, out var entry))
		{
			ownerId = entry.OwnerId;
			return true;
		}

		ownerId = string.Empty;
		return false;
	}

	public bool IsOwnedBy(string itemId, string ownerId)
	{
		ArgumentException.ThrowIfNullOrEmpty(ownerId);
		return TryGetOwnerId(itemId, out var current)
			&& string.Equals(current, ownerId, StringComparison.Ordinal);
	}

	public bool IsPlayerOwned(string itemId) =>
		IsOwnedBy(itemId, StarSystemActorIds.PlayerFleet);

	public IReadOnlyList<ShipInstance> ShipsOwnedBy(string ownerId)
	{
		ArgumentException.ThrowIfNullOrEmpty(ownerId);
		return _entries.Values
			.Where(entry => string.Equals(entry.OwnerId, ownerId, StringComparison.Ordinal))
			.Select(entry => AsShip(entry.Item))
			.ToArray();
	}

	public ShipInstance Get(string shipId) =>
		_entries.TryGetValue(shipId, out var entry)
			? AsShip(entry.Item)
			: throw new KeyNotFoundException($"Ship '{shipId}' is not registered.");

	public bool TryGet(string shipId, out ShipInstance ship)
	{
		if (_entries.TryGetValue(shipId, out var entry))
		{
			ship = AsShip(entry.Item);
			return true;
		}

		ship = null!;
		return false;
	}

	public bool Matches(string shipId, ShipInstance expected)
	{
		ArgumentNullException.ThrowIfNull(expected);
		if (!TryGet(shipId, out var current))
			return false;

		return string.Equals(current.Id, expected.Id, StringComparison.Ordinal)
			&& current.HullPoints == expected.HullPoints
			&& current.ShieldPoints.Matches(expected.ShieldPoints)
			&& LoadoutMatches(current, expected);
	}

	private static bool LoadoutMatches(ShipInstance current, ShipInstance before) =>
		current.Spec.Chassis == before.Spec.Chassis
		&& current.Loadout.MaxHullPoints == before.Loadout.MaxHullPoints
		&& current.Loadout.HullUpgradeTier == before.Loadout.HullUpgradeTier
		&& current.Loadout.ShieldUpgradeTiers.Matches(before.Loadout.ShieldUpgradeTiers)
		&& current.Loadout.MaxShieldPoints.Matches(before.Loadout.MaxShieldPoints)
		&& current.Loadout.InstalledAbilities.SequenceEqual(before.Loadout.InstalledAbilities);

	public void ApplyHandoff(UnitStateHandoff handoff)
	{
		var ship = Get(handoff.Id);
		if (ship.Spec.Chassis != handoff.Chassis)
			throw new InvalidOperationException(
				$"Handoff chassis '{handoff.Chassis}' does not match registry chassis '{ship.Spec.Chassis}' for ship '{handoff.Id}'.");

		ship.HullPoints = handoff.HullPoints;
		ship.ShieldPoints = handoff.ShieldPoints.Clone();
	}

	private static ShipInstance AsShip(IItem item) =>
		item as ShipInstance
		?? throw new InvalidOperationException(
			$"Inventory item '{item.Id}' is not a {nameof(ShipInstance)}.");
}

internal readonly record struct InventorySnapshotEntry(ShipInstance Ship, string OwnerId);
