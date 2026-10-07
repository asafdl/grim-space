using System.Collections.Frozen;
using GrimSpace.Battle.Ids;
using GrimSpace.Core;
using GrimSpace.Battle.Units;
using GrimSpace.Core.Engine;
using GrimSpace.Math.Grid;
using BoundedGrid = GrimSpace.Math.Grid.Grid;
using GrimSpace.Battle.Objectives;

namespace GrimSpace.Battle.World;

/// <summary>
/// Live battlefield world during a fight: units (via <see cref="UnitRegistry"/>), non-units, grid, and timeline.
/// <see cref="BattleWorld.Fork"/> snapshots for preview sims; commit writes back to this instance.
/// </summary>
public sealed class BattleWorld : IWorld<BattleWorld>, IActorStateWorld<State, BattleWorld>
{
	private readonly Dictionary<string, NonUnit> _nonUnits;
	private FrozenSet<Coord> _abilityBlockingCells;

	public UnitRegistry UnitRegistry { get; }
	public IReadOnlyDictionary<string, NonUnit> NonUnits => _nonUnits;
	public FrozenSet<Coord> AbilityBlockingCells => _abilityBlockingCells;
	public BoundedGrid Grid { get; }
	public IReadOnlySet<Coord> BlockedCells { get; }
	public Timeline Timeline { get; }

	public State StateOf(string unitId) => UnitRegistry.UnitOf(unitId).State;

	public string BattleId { get; }
	public EObjective Objective { get; }
	public IReadOnlySet<string> EngagedShipIds { get; }
	public EBattleResult battleResult { get; set; } = EBattleResult.Ongoing;

	public T NonUnitOf<T>(string id) where T : NonUnit => (T)_nonUnits[id];

	public void AddNonUnit(NonUnit nonUnit)
	{
		ArgumentNullException.ThrowIfNull(nonUnit);
		if (!_nonUnits.TryAdd(nonUnit.Id, nonUnit))
			throw new InvalidOperationException($"Non-unit '{nonUnit.Id}' already exists.");

		_abilityBlockingCells = CollectAbilityBlockingCells(_nonUnits.Values);
	}

	public bool RemoveNonUnit(string id)
	{
		if (!_nonUnits.Remove(id))
			return false;

		_abilityBlockingCells = CollectAbilityBlockingCells(_nonUnits.Values);
		return true;
	}

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

	public readonly record struct UnitInArea(Unit Unit, EUnitRelation Relation);

	public IEnumerable<Asteroid> Asteroids => _nonUnits.Values.OfType<Asteroid>();

	public IEnumerable<Hazard> Hazards => _nonUnits.Values.OfType<Hazard>();

	public static HashSet<Coord> TerrainBlockedCells(IEnumerable<NonUnit> terrain)
	{
		var cells = new HashSet<Coord>();
		foreach (var nonUnit in terrain)
		{
			if (!nonUnit.Passable)
				cells.UnionWith(nonUnit.Cells);
		}

		return cells;
	}

	public IEnumerable<NonUnit> NonUnitsOwnedBy(string actorId) =>
		_nonUnits.Values.Where(nonUnit => nonUnit.ActorId == actorId);

	public bool IsCellBlocked(Coord cell)
	{
		if (BlockedCells.Contains(cell))
			return true;

		foreach (var unit in UnitRegistry.All)
		{
			if (unit.State.IsAlive && unit.State.Position == cell)
				return true;
		}

		return false;
	}

	private BattleWorld(
		UnitRegistry unitRegistry,
		Dictionary<string, NonUnit> nonUnits,
		FrozenSet<Coord> abilityBlockingCells,
		BoundedGrid grid,
		IReadOnlySet<Coord> blockedCells,
		Timeline timeline,
		string battleId,
		EObjective objective,
		IReadOnlySet<string> engagedShipIds)
	{
		UnitRegistry = unitRegistry;
		_nonUnits = nonUnits;
		_abilityBlockingCells = abilityBlockingCells;
		Grid = grid;
		BlockedCells = blockedCells;
		Timeline = timeline;
		BattleId = battleId;
		Objective = objective;
		EngagedShipIds = engagedShipIds;
	}

	public static BattleWorld FromSnapshot(
		IReadOnlyList<Unit> roster,
		IReadOnlyDictionary<string, NonUnit> nonUnits,
		BoundedGrid grid,
		IReadOnlySet<Coord> blockedCells,
		string battleId,
		EObjective objective,
		IReadOnlySet<string> engagedShipIds,
		Timeline? timeline = null) =>
		FromRoster(
			roster.Select(CloneForSnapshot).ToList(),
			nonUnits.ToDictionary(),
			grid,
			blockedCells,
			battleId,
			objective,
			engagedShipIds,
			timeline);

	public static BattleWorld FromLive(
		IReadOnlyList<Unit> roster,
		Dictionary<string, NonUnit> nonUnits,
		BoundedGrid grid,
		IReadOnlySet<Coord> blockedCells,
		string battleId,
		EObjective objective,
		IReadOnlySet<string> engagedShipIds,
		Timeline? timeline = null) =>
		FromRoster(
			roster,
			nonUnits.ToDictionary(),
			grid,
			blockedCells,
			battleId,
			objective,
			engagedShipIds,
			timeline);

	private static BattleWorld FromRoster(
		IReadOnlyList<Unit> roster,
		Dictionary<string, NonUnit> nonUnits,
		BoundedGrid grid,
		IReadOnlySet<Coord> blockedCells,
		string battleId,
		EObjective objective,
		IReadOnlySet<string> engagedShipIds,
		Timeline? timeline)
	{
		var units = new UnitRegistry();
		foreach (var unit in roster)
			units.Add(unit);

		return new BattleWorld(
			units,
			nonUnits,
			CollectAbilityBlockingCells(nonUnits.Values),
			grid,
			blockedCells,
			timeline ?? new Timeline(),
			battleId,
			objective,
			engagedShipIds);
	}

	private static Unit CloneForSnapshot(Unit unit) =>
		new(unit.State.Clone(), unit.ExecutionAgent, unit.Team);

	public BattleWorld Fork() => Fork(Timeline.Clone());

	public BattleWorld ForkForSimulation() => Fork(Timeline.CloneSnapshot());

	private BattleWorld Fork(Timeline timeline) =>
		new(
			UnitRegistry.CloneForFork(),
			_nonUnits.ToDictionary(),
			_abilityBlockingCells,
			Grid,
			BlockedCells,
			timeline,
			BattleId,
			Objective,
			EngagedShipIds);

	private static FrozenSet<Coord> CollectAbilityBlockingCells(IEnumerable<NonUnit> nonUnits)
	{
		var cells = new HashSet<Coord>();
		foreach (var nonUnit in nonUnits)
		{
			if (nonUnit.BlocksAbilities)
				cells.UnionWith(nonUnit.Cells);
		}

		return cells.ToFrozenSet();
	}
}

public enum EUnitRelation
{
	Self,
	Ally,
	Opponent,
}
