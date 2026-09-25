using GrimSpace.Battle.Objectives;
using GrimSpace.Units;
using GrimSpace.Units.Enums;
using GrimSpace.Units.Loadouts.Abilities;

namespace GrimSpace.Run;

public sealed class RunShipRegistry : IShipRegistryReader
{
	private readonly Dictionary<string, ShipInstance> _ships = new(StringComparer.Ordinal);

	public IReadOnlyCollection<ShipInstance> All => _ships.Values;

	public void Register(ShipInstance ship)
	{
		ArgumentNullException.ThrowIfNull(ship);
		if (_ships.ContainsKey(ship.Id))
			return;

		_ships[ship.Id] = ship;
	}

	public void Update(ShipInstance ship)
	{
		ArgumentNullException.ThrowIfNull(ship);
		if (!_ships.ContainsKey(ship.Id))
			throw new KeyNotFoundException($"Ship '{ship.Id}' is not registered.");

		_ships[ship.Id] = ship;
	}

	public void Register(ShipSpawnDeclaration declaration) =>
		Register(ShipInstance.FromCatalog(declaration.ShipId, declaration.Chassis));

	public ShipInstance Get(string shipId) =>
		_ships.TryGetValue(shipId, out var ship)
			? ship
			: throw new KeyNotFoundException($"Ship '{shipId}' is not registered.");

	public bool TryGet(string shipId, out ShipInstance ship) => _ships.TryGetValue(shipId, out ship!);

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
}
