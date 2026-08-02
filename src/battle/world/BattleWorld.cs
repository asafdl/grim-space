using GrimSpace.Battle.Ids;
using GrimSpace.Core;
using GrimSpace.Battle.Units;
using GrimSpace.Core.Engine;
using GrimSpace.Math.Grid;
using GrimSpace.Units.Enums;
using BoundedGrid = GrimSpace.Math.Grid.Grid;

namespace GrimSpace.Battle.World;

/// <summary>
/// Live battlefield world during a fight: units, hazards, grid, and timeline.
/// <see cref="BattleWorld.Fork"/> snapshots for preview sims; commit writes back to this instance.
/// Terrain vs turn hazards are partitioned by <see cref="EntityIds.World"/> ownership.
/// </summary>
public sealed class BattleWorld : IWorld<BattleWorld>, IActorStateWorld<State, BattleWorld>
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
	public Timeline Timeline { get; }

	public State StateOf(string unitId) => _units[unitId].State;

	public Unit UnitOf(string unitId) => _units[unitId];

	public T NonUnitOf<T>(string id) where T : NonUnit => (T)_nonUnits[id];

	public IEnumerable<Unit> UnitsExcept(string unitId) =>
		_units.Values.Where(unit => unit.State.Id != unitId);

	public IEnumerable<UnitInArea> UnitsInCells(string actorId, IEnumerable<Coord> cells)
	{
		var cellSet = cells as IReadOnlySet<Coord> ?? cells.ToHashSet();
		var actor = _units[actorId];

		foreach (var unit in _units.Values)
		{
			if (!unit.State.IsAlive || !cellSet.Contains(unit.State.Position))
				continue;

			yield return new UnitInArea(unit, RelationTo(actor, unit));
		}
	}

	public bool AnyOpponentInCells(string actorId, IEnumerable<Coord> cells) =>
		UnitsInCells(actorId, cells).Any(entry => entry.Relation == EUnitRelation.Opponent);

	private static EUnitRelation RelationTo(Unit actor, Unit unit)
	{
		if (unit.State.Id == actor.State.Id)
			return EUnitRelation.Self;

		return unit.Controller == actor.Controller ? EUnitRelation.Ally : EUnitRelation.Opponent;
	}

	public IEnumerable<Hazard> Hazards => _nonUnits.Values.OfType<Hazard>();

	public IEnumerable<Hazard> TerrainHazards =>
		Hazards.Where(hazard => hazard.ActorId == EntityIds.World);

	public IEnumerable<Hazard> TurnHazards =>
		Hazards.Where(hazard => hazard.ActorId != EntityIds.World);

	public static HashSet<Coord> TerrainBlockedCells(IEnumerable<Hazard> terrain)
	{
		var cells = new HashSet<Coord>();
		foreach (var hazard in terrain)
		{
			if (!hazard.Passable)
				cells.UnionWith(hazard.Cells);
		}

		return cells;
	}

	public IEnumerable<NonUnit> NonUnitsOwnedBy(string actorId) =>
		_nonUnits.Values.Where(nonUnit => nonUnit.ActorId == actorId);

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

	private BattleWorld(
		Dictionary<string, Unit> units,
		Dictionary<string, NonUnit> nonUnits,
		BoundedGrid grid,
		IReadOnlySet<Coord> blockedCells,
		Timeline timeline)
	{
		_units = units;
		_nonUnits = nonUnits;
		Grid = grid;
		BlockedCells = blockedCells;
		Timeline = timeline;
	}

	public static BattleWorld FromSnapshot(
		IReadOnlyList<Unit> roster,
		IReadOnlyDictionary<string, NonUnit> nonUnits,
		BoundedGrid grid,
		IReadOnlySet<Coord> blockedCells,
		Timeline? timeline = null)
	{
		var board = new BattleWorld(
			roster.ToDictionary(unit => unit.State.Id, CloneForSnapshot),
			nonUnits.ToDictionary(pair => pair.Key, pair => CloneNonUnit(pair.Value)),
			grid,
			blockedCells,
			timeline ?? new Timeline());

		foreach (var id in roster.Select(unit => unit.State.Id).Concat(nonUnits.Keys))
			board._idRegistry.Register(id);

		return board;
	}

	public static BattleWorld FromLive(
		IReadOnlyList<Unit> roster,
		IDictionary<string, NonUnit> nonUnits,
		BoundedGrid grid,
		IReadOnlySet<Coord> blockedCells,
		Timeline? timeline = null)
	{
		var board = new BattleWorld(
			roster.ToDictionary(unit => unit.State.Id, unit => unit),
			(Dictionary<string, NonUnit>)nonUnits,
			grid,
			blockedCells,
			timeline ?? new Timeline());

		foreach (var id in roster.Select(unit => unit.State.Id).Concat(nonUnits.Keys))
			board._idRegistry.Register(id);

		return board;
	}

	private static Unit CloneForSnapshot(Unit unit)
	{
		var cloned = unit.State.Clone();
		return unit.Controller switch
		{
			EController.Player => new Units.Player(cloned),
			EController.Enemy => new EnemyUnit(cloned),
			_ => throw new ArgumentOutOfRangeException(nameof(unit)),
		};
	}

	public BattleWorld Fork() =>
		FromSnapshot(Units.Values.ToList(), NonUnits, Grid, BlockedCells, Timeline.Clone());

	private static NonUnit CloneNonUnit(NonUnit nonUnit) =>
		nonUnit switch
		{
			Hazard hazard => hazard.Clone(),
			_ => throw new ArgumentOutOfRangeException(nameof(nonUnit)),
		};
}
