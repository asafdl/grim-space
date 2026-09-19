using GrimSpace.Battle.Objectives;
using GrimSpace.Units;
using GrimSpace.Units.Enums;

namespace GrimSpace.Run;

public sealed class RunShipRegistry
{
	private readonly Dictionary<string, RunShip> _ships = new(StringComparer.Ordinal);

	public IReadOnlyCollection<RunShip> All => _ships.Values;

	public void Register(RunShip ship)
	{
		ArgumentNullException.ThrowIfNull(ship);
		if (_ships.TryGetValue(ship.Id, out var existing))
		{
			if (!ConfigurationsMatch(existing.Configuration, ship.Configuration))
				throw new InvalidOperationException(
					$"Ship '{ship.Id}' is already registered with a different configuration.");

			return;
		}

		_ships[ship.Id] = ship;
	}

	public void Register(ShipSpawnDeclaration declaration) =>
		Register(RunShip.CreateDefault(declaration.ShipId, declaration.Chassis));

	public RunShip Get(string shipId) =>
		_ships.TryGetValue(shipId, out var ship)
			? ship
			: throw new KeyNotFoundException($"Ship '{shipId}' is not registered.");

	public bool TryGet(string shipId, out RunShip ship) => _ships.TryGetValue(shipId, out ship!);

	public ShipSnapshot Snapshot(string shipId) => Get(shipId).ToSnapshot();

	public IReadOnlyList<ShipSnapshot> SnapshotsForMemberIds(IEnumerable<string> shipIds) =>
		shipIds.Select(Snapshot).ToArray();

	public void ApplyHandoff(UnitStateHandoff handoff)
	{
		var ship = Get(handoff.Id);
		if (ship.Configuration.Chassis != handoff.Chassis)
			throw new InvalidOperationException(
				$"Handoff chassis '{handoff.Chassis}' does not match registry chassis '{ship.Configuration.Chassis}' for ship '{handoff.Id}'.");

		ship.HullPoints = handoff.HullPoints;
		ship.ShieldPoints = handoff.ShieldPoints.Clone();
	}

	private static bool ConfigurationsMatch(ShipConfiguration left, ShipConfiguration right) =>
		left.Chassis == right.Chassis
		&& left.MaxHullPoints == right.MaxHullPoints
		&& left.Defenses.Matches(right.Defenses)
		&& left.AbilityMounts.ToHashSet().SetEquals(right.AbilityMounts);
}
