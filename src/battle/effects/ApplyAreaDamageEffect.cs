using GrimSpace.Battle.Abilities;
using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.Spatial;
using GrimSpace.Battle.Units;
using GrimSpace.Battle.World;
using GrimSpace.Core.Actions;
using GrimSpace.Math;
using GrimSpace.Math.Grid;

namespace GrimSpace.Battle.Effects;

public sealed class ApplyAreaDamageEffect(
	EImpactCause cause,
	IReadOnlySet<Coord> cells,
	int damage,
	Coord origin,
	int shieldDamageBonus = 0,
	int adjacentTargetDamageBonus = 0) : IEffect<BattleWorld, ActorRuntime>
{
	public EImpactCause Cause => cause;
	public IReadOnlySet<Coord> Cells => cells;
	public int Damage => damage;
	public Coord Origin => origin;
	public int ShieldDamageBonus => shieldDamageBonus;
	public int AdjacentTargetDamageBonus => adjacentTargetDamageBonus;

	public ApplyAreaDamageEffect WithAdditionalShieldDamage(int amount) =>
		new(
			cause,
			cells,
			damage,
			origin,
			shieldDamageBonus + amount,
			adjacentTargetDamageBonus);

	public ApplyAreaDamageEffect WithAdjacentTargetDamageBonus(int amount) =>
		new(
			cause,
			cells,
			damage,
			origin,
			shieldDamageBonus,
			adjacentTargetDamageBonus + amount);

	private Dictionary<string, UnitCombatSnapshot> _snapshots = [];

	public IReadOnlyList<IRecord> Apply(BattleWorld world, ActorRuntime runtime, string actorId)
	{
		_snapshots = new Dictionary<string, UnitCombatSnapshot>();
		var source = UnitRegistry.For(world).UnitOf(actorId);
		var records = new List<IRecord>
		{
			new Record<AreaDamageFacts>(new AreaDamageFacts(actorId, cause, origin, cells)),
		};
		foreach (var unit in UnitRegistry.For(world).All)
		{
			if (!unit.State.IsAlive || !cells.Contains(unit.State.Position))
				continue;

			_snapshots[unit.State.Id] = UnitCombatSnapshot.Capture(unit.State);
			var isAlliedSource = source.RelationTo(unit) == EUnitRelation.Ally;
			if (ApplyTo(unit.State, actorId, isAlliedSource) is { } impact)
				records.Add(new Record<ImpactFacts>(impact));
		}

		return records;
	}

	public void Undo(BattleWorld world, ActorRuntime runtime, string actorId)
	{
		foreach (var (unitId, snapshot) in _snapshots)
			snapshot.Restore(world.StateOf(unitId));
	}

	private ImpactFacts? ApplyTo(State unit, string actorId, bool isAlliedSource)
	{
		var face = BodyFrame.From(unit).HitFaceFrom(origin);
		var shieldBefore = Defense.EffectiveShield(unit, face);
		var hullBefore = unit.HullPoints;
		var appliedDamage = damage;
		if (origin.ManhattanDistanceTo(unit.Position) == 1)
			appliedDamage += adjacentTargetDamageBonus;
		var appliedShieldDamageBonus = shieldDamageBonus;
		if (isAlliedSource && unit.InTheFrayActive)
		{
			var reducedShieldDamage = HalfRoundedUp(appliedDamage + appliedShieldDamageBonus);
			appliedDamage = HalfRoundedUp(appliedDamage);
			appliedShieldDamageBonus = reducedShieldDamage - appliedDamage;
		}

		if (damage > 0
			&& unit.DefensiveManeuversMpSpentLastTurn == 0
			&& RollsDeflection(unit))
		{
			appliedDamage--;
		}

		if (appliedDamage > 0)
			Defense.ApplyDamage(unit, appliedDamage, face, appliedShieldDamageBonus);

		var shieldAfter = Defense.EffectiveShield(unit, face);
		var shieldDamage = shieldBefore - shieldAfter;
		var hullDamage = hullBefore - unit.HullPoints;
		if (shieldDamage == 0 && hullDamage == 0)
			return null;

		return new ImpactFacts(
			SourceId: actorId,
			TargetId: unit.Id,
			Cause: cause,
			Face: face,
			ShieldDamage: shieldDamage,
			HullDamage: hullDamage);
	}

	private static int HalfRoundedUp(int amount) => (amount + 1) / 2;

	private bool RollsDeflection(State unit)
	{
		var rollIndex = unit.DefensiveManeuversRollIndex++;
		var random = new StableRandom(
			new StableSeedMixer()
				.Add(unit.Id)
				.Add(rollIndex)
				.Add((int)cause)
				.Value);
		return random.NextDouble() < 0.25;
	}
}
