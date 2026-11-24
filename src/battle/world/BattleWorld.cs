using System.Collections.Frozen;
using System.Collections.Immutable;
using GrimSpace.Battle;
using GrimSpace.Battle.ActivationRound;
using GrimSpace.Battle.Ids;
using GrimSpace.Core;
using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.Statuses;
using GrimSpace.Battle.Units;
using GrimSpace.Core.Engine;
using GrimSpace.Math.Grid;
using BoundedGrid = GrimSpace.Math.Grid.Grid;
using GrimSpace.Battle.Objectives;
using GrimSpace.Core.Actions;
using GrimSpace.Battle.Effects;

namespace GrimSpace.Battle.World;

/// <summary>
/// Live battlefield world during a fight: units (via <see cref="UnitRegistry"/>), non-units, grid, and timeline.
/// <see cref="BattleWorld.Fork"/> snapshots for preview sims; commit writes back to this instance.
/// </summary>
public sealed class BattleWorld
	: IWorld<BattleWorld>,
		IActorStateWorld<State, BattleWorld>,
		IResolvedEffectPass<BattleWorld, ActorRuntime>
{
	private readonly Dictionary<string, NonUnit> _nonUnits;
	private FrozenDictionary<Coord, ImmutableArray<NonUnit>> _nonUnitsByCell;
	private FrozenSet<Coord> _abilityBlockingCells;

	public UnitRegistry UnitRegistry { get; }
	public PerUnitActivationRoundLists ActivationRoundLists { get; }
	public IReadOnlyDictionary<string, NonUnit> NonUnits => _nonUnits;
	public FrozenSet<Coord> AbilityBlockingCells => _abilityBlockingCells;
	public BoundedGrid Grid { get; }
	public IReadOnlySet<Coord> BlockedCells { get; }
	public Timeline Timeline { get; }

	public State StateOf(string unitId) => UnitRegistry.UnitOf(unitId).State;

   // TODO: What is this? why would the world be in charge of rewriting effects? this is shit
	IReadOnlyList<IEffect<BattleWorld, ActorRuntime>> IResolvedEffectPass<BattleWorld, ActorRuntime>.RewriteResolvedEffects(
		IAction action,
		BattleWorld world,
		ActorRuntime runtime,
		IReadOnlyList<IEffect<BattleWorld, ActorRuntime>> effects)
	{
		var rewritten = BattleStatuses.RewriteOutgoing(action, world, effects);
		var hitCells = new HashSet<Coord>();
		foreach (var effect in rewritten)
		{
			switch (effect)
			{
				case ApplyAreaDamageEffect area when area.Damage > 0 || area.ShieldDamageBonus > 0:
					hitCells.UnionWith(area.Cells.Where(cell => cell != area.Origin));
					break;
				case ApplyChainLightningDamageEffect chain
					when chain.BaseDamage > 0 || chain.ShieldDamageBonus > 0:
					hitCells.UnionWith(chain.BeamCells.Where(cell => cell != chain.Origin));
					hitCells.UnionWith(chain.Hops
						.Select(hop => hop.Position)
						.Where(cell => cell != chain.Origin));
					break;
			}
		}

		var resolved = rewritten
			.Select(effect => effect is ApplyAreaDamageEffect or ApplyChainLightningDamageEffect
				? new ApplyKillTriggersEffect(effect)
				: effect)
			.ToList();
		if (hitCells.Any(cell => world.AsteroidBeltAt(cell) is not null))
			resolved.Add(new ErodeAsteroidBeltCoverEffect(hitCells));

		return resolved;
	}

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

		_nonUnitsByCell = IndexNonUnitsByCell(_nonUnits.Values);
		_abilityBlockingCells = CollectAbilityBlockingCells(_nonUnits.Values);
	}

	public bool RemoveNonUnit(string id)
	{
		if (!_nonUnits.Remove(id))
			return false;

		_nonUnitsByCell = IndexNonUnitsByCell(_nonUnits.Values);
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

	public int CountUnitsInRange(
		string actorId,
		EUnitRelation relation,
		int minDistance,
		int maxDistance = int.MaxValue)
	{
		ArgumentOutOfRangeException.ThrowIfNegative(minDistance);
		if (maxDistance < minDistance)
			throw new ArgumentOutOfRangeException(nameof(maxDistance));

		var actor = UnitRegistry.UnitOf(actorId);
		var origin = actor.State.Position;
		var count = 0;
		foreach (var unit in UnitRegistry.All)
		{
			if (!unit.State.IsAlive || actor.RelationTo(unit) != relation)
				continue;
			var distance = origin.ManhattanDistanceTo(unit.State.Position);
			if (distance < minDistance || distance > maxDistance)
				continue;
			count++;
		}

		return count;
	}

	public readonly record struct UnitInArea(Unit Unit, EUnitRelation Relation);

	public IEnumerable<Asteroid> Asteroids => _nonUnits.Values.OfType<Asteroid>();

	public IEnumerable<Hazard> Hazards => _nonUnits.Values.OfType<Hazard>();

	public CryoPlume? CryoPlumeAt(Coord cell)
	{
		if (!_nonUnitsByCell.TryGetValue(cell, out var nonUnits))
			return null;

		foreach (var nonUnit in nonUnits)
		{
			if (nonUnit is CryoPlume plume)
				return plume;
		}

		return null;
	}

	public IonStorm? IonStormAt(Coord cell)
	{
		if (!_nonUnitsByCell.TryGetValue(cell, out var nonUnits))
			return null;

		foreach (var nonUnit in nonUnits)
		{
			if (nonUnit is IonStorm storm)
				return storm;
		}

		return null;
	}

	public AsteroidBelt? AsteroidBeltAt(Coord cell)
	{
		if (!Grid.IsInBounds(cell))
			throw new ArgumentOutOfRangeException(nameof(cell), "Cell must be inside the battle grid.");
		if (!_nonUnitsByCell.TryGetValue(cell, out var nonUnits))
			return null;

		foreach (var nonUnit in nonUnits)
		{
			if (nonUnit is AsteroidBelt belt)
				return belt;
		}

		return null;
	}

	public bool IsTranslationAssisted(Coord source, Coord heading)
	{
		if (!_nonUnitsByCell.TryGetValue(source, out var nonUnits))
			return false;

		foreach (var nonUnit in nonUnits)
		{
			if (nonUnit is GravityWell well
				&& well.AssistedDirectionsByCell.TryGetValue(source, out var directions)
				&& directions.Contains(heading))
				return true;
		}

		return false;
	}

	public static HashSet<Coord> TerrainBlockedCells(IEnumerable<NonUnit> terrain)
	{
		var cells = new HashSet<Coord>();
		foreach (var nonUnit in terrain)
			cells.UnionWith(nonUnit.MovementBlockingCells);

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
		PerUnitActivationRoundLists activationRoundLists,
		FrozenDictionary<Coord, ImmutableArray<NonUnit>> nonUnitsByCell,
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
		ActivationRoundLists = activationRoundLists;
		_nonUnitsByCell = nonUnitsByCell;
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
			new PerUnitActivationRoundLists(),
			IndexNonUnitsByCell(nonUnits.Values),
			CollectAbilityBlockingCells(nonUnits.Values),
			grid,
			blockedCells,
			timeline ?? new Timeline(),
			battleId,
			objective,
			engagedShipIds);
	}

	private static Unit CloneForSnapshot(Unit unit) =>
		new(unit.State.Clone(), unit.ExecutionAgent, unit.Team, unit.Faction);

	public BattleWorld Fork() => Fork(Timeline.Clone());

	public BattleWorld ForkForSimulation() => Fork(Timeline.CloneSnapshot());

	private BattleWorld Fork(Timeline timeline) =>
		new(
			UnitRegistry.CloneForFork(),
			_nonUnits.ToDictionary(),
			ActivationRoundLists.CloneForFork(),
			_nonUnitsByCell,
			_abilityBlockingCells,
			Grid,
			BlockedCells,
			timeline,
			BattleId,
			Objective,
			EngagedShipIds);

	private static FrozenDictionary<Coord, ImmutableArray<NonUnit>> IndexNonUnitsByCell(
		IEnumerable<NonUnit> nonUnits)
	{
		var cells = new Dictionary<Coord, List<NonUnit>>();
		foreach (var nonUnit in nonUnits)
		foreach (var cell in nonUnit.Cells)
		{
			if (!cells.TryGetValue(cell, out var occupants))
			{
				occupants = [];
				cells.Add(cell, occupants);
			}

			occupants.Add(nonUnit);
		}

		return cells.ToFrozenDictionary(entry => entry.Key, entry => entry.Value.ToImmutableArray());
	}

	private static FrozenSet<Coord> CollectAbilityBlockingCells(IEnumerable<NonUnit> nonUnits)
	{
		var cells = new HashSet<Coord>();
		foreach (var nonUnit in nonUnits)
			cells.UnionWith(nonUnit.AbilityBlockingCells);

		return cells.ToFrozenSet();
	}
}

public enum EUnitRelation
{
	Self,
	Ally,
	Opponent,
}
