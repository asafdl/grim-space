using GrimSpace.Battle.Board;
using GrimSpace.Battle.Ids;
using GrimSpace.Battle.Units;
using GrimSpace.Math.Grid;
using GrimSpace.Units.Enums;
using BoundedGrid = GrimSpace.Math.Grid.Grid;

namespace GrimSpace.Core.Actions.Battle;

/// <summary>
/// Mutable battlefield for action resolution. Each turn opens a planning board
/// cloned from current unit state; commit applies the queued actions to live state.
/// </summary>
public sealed class BattleBoard
{
	private readonly Dictionary<string, Unit> _units;
	private readonly Dictionary<string, NonUnit> _nonUnits;
	private readonly UnitIdRegistry _idRegistry = new();

	public IReadOnlyDictionary<string, Unit> Units => _units;
	public IReadOnlyDictionary<string, NonUnit> NonUnits => _nonUnits;
	public IDictionary<string, NonUnit> MutableNonUnits => _nonUnits;
	public UnitIdRegistry IdRegistry => _idRegistry;
	public BoundedGrid Grid { get; }
	public IReadOnlySet<Coord> BlockedCells { get; }

	public State StateOf(string unitId) => _units[unitId].State;

	public Unit UnitOf(string unitId) => _units[unitId];

	public T NonUnitOf<T>(string id) where T : NonUnit => (T)_nonUnits[id];

	public IEnumerable<Unit> UnitsExcept(string unitId) =>
		_units.Values.Where(unit => unit.State.Id != unitId);

	public IEnumerable<Hazard> Hazards => _nonUnits.Values.OfType<Hazard>();

	public IEnumerable<Hazard> TurnHazards =>
		Hazards.Where(hazard => hazard.OwnerId != EntityIds.Board);

	public IEnumerable<NonUnit> NonUnitsOwnedBy(string actorId) =>
		_nonUnits.Values.Where(nonUnit => nonUnit.OwnerId == actorId);

	public HashSet<Coord> OccupiedCellsFor(string actorId)
	{
		var cells = new HashSet<Coord>();
		foreach (var unit in UnitsExcept(actorId))
			cells.Add(unit.State.Position);

		foreach (var nonUnit in _nonUnits.Values)
			cells.UnionWith(nonUnit.Cells);

		return cells;
	}

	public HashSet<Coord> BlockedFor(string actorId)
	{
		var blocked = new HashSet<Coord>(BlockedCells);
		foreach (var unit in UnitsExcept(actorId))
			blocked.Add(unit.State.Position);

		return blocked;
	}

	private BattleBoard(
		Dictionary<string, Unit> units,
		Dictionary<string, NonUnit> nonUnits,
		BoundedGrid grid,
		IReadOnlySet<Coord> blockedCells)
	{
		_units = units;
		_nonUnits = nonUnits;
		Grid = grid;
		BlockedCells = blockedCells;
	}

	public static BattleBoard FromSnapshot(
		IReadOnlyList<Unit> roster,
		IReadOnlyDictionary<string, NonUnit> nonUnits,
		BoundedGrid grid,
		IReadOnlySet<Coord> blockedCells)
	{
		var board = new BattleBoard(
			roster.ToDictionary(unit => unit.State.Id, CloneForSnapshot),
			nonUnits.ToDictionary(pair => pair.Key, pair => CloneNonUnit(pair.Value)),
			grid,
			blockedCells);

		foreach (var id in roster.Select(unit => unit.State.Id).Concat(nonUnits.Keys))
			board._idRegistry.Register(id);

		return board;
	}

	public static BattleBoard FromLive(
		IReadOnlyList<Unit> roster,
		IDictionary<string, NonUnit> nonUnits,
		BoundedGrid grid,
		IReadOnlySet<Coord> blockedCells)
	{
		var board = new BattleBoard(
			roster.ToDictionary(unit => unit.State.Id, unit => unit),
			(Dictionary<string, NonUnit>)nonUnits,
			grid,
			blockedCells);

		foreach (var id in roster.Select(unit => unit.State.Id).Concat(nonUnits.Keys))
			board._idRegistry.Register(id);

		return board;
	}

	private static Unit CloneForSnapshot(Unit unit)
	{
		var cloned = unit.State.Clone();
		return unit.Controller switch
		{
			EController.Player => new Player(cloned, unit.Movement),
			EController.Enemy => new EnemyUnit(cloned, unit.Movement),
			_ => throw new ArgumentOutOfRangeException(nameof(unit)),
		};
	}

	private static NonUnit CloneNonUnit(NonUnit nonUnit) =>
		nonUnit switch
		{
			Hazard hazard => hazard.Clone(),
			_ => throw new ArgumentOutOfRangeException(nameof(nonUnit)),
		};
}
