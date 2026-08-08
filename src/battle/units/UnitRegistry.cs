using GrimSpace.Battle.World;
using GrimSpace.Core;
using GrimSpace.Units.Enums;

namespace GrimSpace.Battle.Units;

/// <summary>
/// Per-world source of truth for units and activation order.
/// Access via <see cref="For"/>.
/// </summary>
public sealed class UnitRegistry
{
	private readonly Dictionary<string, Unit> _units = new(StringComparer.Ordinal);
	private readonly HashSet<string> _enlisted = new(StringComparer.Ordinal);
	private readonly Dictionary<string, LinkedListNode<string>> _nodes = new(StringComparer.Ordinal);
	private readonly PriorityLinkedList<string> _turnOrder;

	public UnitRegistry() =>
		_turnOrder = new PriorityLinkedList<string>(Rank);

	public static UnitRegistry For(BattleWorld world) => world.UnitRegistry;

	public LinkedListNode<string>? First => _turnOrder.First;

	public IEnumerable<Unit> All => _units.Values;

	public IEnumerable<string> Ids => _units.Keys;

	public Unit UnitOf(string unitId) => _units[unitId];

	public bool TryGet(string unitId, out Unit unit) => _units.TryGetValue(unitId, out unit!);

	public bool TryGet(EType type, out Unit unit)
	{
		foreach (var candidate in _units.Values)
		{
			if (candidate.State.Type != type)
				continue;

			unit = candidate;
			return true;
		}

		unit = null!;
		return false;
	}

	public bool Contains(string unitId) => _units.ContainsKey(unitId);

	public void Add(Unit unit)
	{
		var id = unit.State.Id;
		_units[id] = unit;
		if (!_enlisted.Add(id))
			return;

		_nodes[id] = _turnOrder.Add(id);
	}

	public void Remove(string unitId)
	{
		_units.Remove(unitId);
		if (!_enlisted.Remove(unitId))
			return;

		if (_nodes.Remove(unitId, out var node))
			_turnOrder.Remove(node);
	}

	public IEnumerable<Unit> Except(string unitId) =>
		_units.Values.Where(unit => unit.State.Id != unitId);

	public UnitRegistry CloneForFork()
	{
		var clone = new UnitRegistry();
		for (var node = First; node is not null; node = node.Next)
		{
			if (_units.TryGetValue(node.Value, out var unit))
				clone.Add(CloneUnit(unit));
		}

		return clone;
	}

	private int Rank(string unitId)
	{
		var unit = _units[unitId];
		return unit.State.Type == EType.Torpedo ? 2
			: unit.Alliance.Team == ETeam.Player ? 0
			: 1;
	}

	private static Unit CloneUnit(Unit unit) =>
		new(unit.Alliance, unit.State.Clone(), unit.ExecutionAgent);
}
