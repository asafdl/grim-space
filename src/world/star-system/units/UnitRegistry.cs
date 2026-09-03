using GrimSpace.World.StarSystem;

namespace GrimSpace.World.StarSystem.Units;

public sealed class UnitRegistry
{
	private readonly Dictionary<string, Unit> _units = new(StringComparer.Ordinal);

	public static UnitRegistry For(StarMap world) => world.UnitRegistry;

	public IEnumerable<Unit> All => _units.Values;

	public IEnumerable<string> Ids => _units.Keys;

	public Unit UnitOf(string unitId) => _units[unitId];

	public bool TryGet(string unitId, out Unit unit) => _units.TryGetValue(unitId, out unit!);

	public bool Contains(string unitId) => _units.ContainsKey(unitId);

	public void Add(Unit unit) =>
		_units[unit.State.Id] = unit;

	public bool Remove(string unitId) => _units.Remove(unitId);

	public UnitRegistry CloneForFork()
	{
		var clone = new UnitRegistry();
		foreach (var unit in _units.Values)
			clone.Add(CloneUnit(unit));

		return clone;
	}

	private static Unit CloneUnit(Unit unit) =>
		new(unit.State.Clone(), unit.Runtime.Fork());
}
