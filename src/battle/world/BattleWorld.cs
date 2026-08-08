using GrimSpace.Battle.Ids;
using GrimSpace.Core;
using GrimSpace.Battle.Units;
using GrimSpace.Core.Engine;
using GrimSpace.Math.Grid;
using BoundedGrid = GrimSpace.Math.Grid.Grid;

namespace GrimSpace.Battle.World;

/// <summary>
/// Live battlefield world during a fight: units (via <see cref="UnitRegistry"/>), hazards, grid, and timeline.
/// <see cref="BattleWorld.Fork"/> snapshots for preview sims; commit writes back to this instance.
/// Terrain vs turn hazards are partitioned by <see cref="EntityIds.World"/> ownership.
/// </summary>
public sealed class BattleWorld : IWorld<BattleWorld>, IActorStateWorld<State, BattleWorld>
{
	private readonly Dictionary<string, NonUnit> _nonUnits;
	private readonly UnitIdRegistry _idRegistry = new();

	public UnitRegistry UnitRegistry { get; }
	public IReadOnlyDictionary<string, NonUnit> NonUnits => _nonUnits;
	public IDictionary<string, NonUnit> MutableNonUnits => _nonUnits;
	public UnitIdRegistry IdRegistry => _idRegistry;
	public BoundedGrid Grid { get; }
	public IReadOnlySet<Coord> BlockedCells { get; }
	public Timeline Timeline { get; }

	public State StateOf(string unitId) => UnitRegistry.UnitOf(unitId).State;

	public T NonUnitOf<T>(string id) where T : NonUnit => (T)_nonUnits[id];

	public IEnumerable<UnitInArea> UnitsInCells(string actorId, IEnumerable<Coord> cells)
	{
		var cellSet = cells as IReadOnlySet<Coord> ?? cells.ToHashSet();
		var units = UnitRegistry;
		var actor = units.UnitOf(actorId);

		foreach (var unit in units.All)
		{
			if (!unit.State.IsAlive || !cellSet.Contains(unit.State.Position))
				continue;

			yield return new UnitInArea(unit, actor.RelationTo(unit));
		}
	}

	public bool AnyOpponentInCells(string actorId, IEnumerable<Coord> cells) =>
		UnitsInCells(actorId, cells).Any(entry => entry.Relation == EUnitRelation.Opponent);

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
		foreach (var unit in UnitRegistry.Except(actorId))
			cells.Add(unit.State.Position);

		foreach (var nonUnit in _nonUnits.Values)
			cells.UnionWith(nonUnit.Cells);

		return cells;
	}

	public HashSet<Coord> BlockedFor(string actorId)
	{
		var blocked = new HashSet<Coord>(BlockedCells);
		foreach (var unit in UnitRegistry.Except(actorId))
			blocked.Add(unit.State.Position);

		return blocked;
	}

	private BattleWorld(
		UnitRegistry unitRegistry,
		Dictionary<string, NonUnit> nonUnits,
		BoundedGrid grid,
		IReadOnlySet<Coord> blockedCells,
		Timeline timeline)
	{
		UnitRegistry = unitRegistry;
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
		Timeline? timeline = null) =>
		FromRoster(
			roster.Select(CloneForSnapshot).ToList(),
			nonUnits.ToDictionary(pair => pair.Key, pair => CloneNonUnit(pair.Value)),
			grid,
			blockedCells,
			timeline);

	public static BattleWorld FromLive(
		IReadOnlyList<Unit> roster,
		IDictionary<string, NonUnit> nonUnits,
		BoundedGrid grid,
		IReadOnlySet<Coord> blockedCells,
		Timeline? timeline = null) =>
		FromRoster(
			roster,
			(Dictionary<string, NonUnit>)nonUnits,
			grid,
			blockedCells,
			timeline);

	private static BattleWorld FromRoster(
		IReadOnlyList<Unit> roster,
		Dictionary<string, NonUnit> nonUnits,
		BoundedGrid grid,
		IReadOnlySet<Coord> blockedCells,
		Timeline? timeline)
	{
		var units = new UnitRegistry();
		foreach (var unit in roster)
			units.Add(unit);

		var board = new BattleWorld(
			units,
			nonUnits,
			grid,
			blockedCells,
			timeline ?? new Timeline());

		foreach (var id in units.Ids.Concat(nonUnits.Keys))
			board._idRegistry.Register(id);

		return board;
	}

	private static Unit CloneForSnapshot(Unit unit) =>
		new(unit.Alliance, unit.State.Clone(), unit.ExecutionAgent);

	public BattleWorld Fork()
	{
		var fork = new BattleWorld(
			UnitRegistry.CloneForFork(),
			_nonUnits.ToDictionary(pair => pair.Key, pair => CloneNonUnit(pair.Value)),
			Grid,
			BlockedCells,
			Timeline.Clone());

		foreach (var id in UnitRegistry.Ids.Concat(_nonUnits.Keys))
			fork._idRegistry.Register(id);

		return fork;
	}
	private static NonUnit CloneNonUnit(NonUnit nonUnit) =>
		nonUnit switch
		{
			Hazard hazard => hazard.Clone(),
			_ => throw new ArgumentOutOfRangeException(nameof(nonUnit)),
		};
}
