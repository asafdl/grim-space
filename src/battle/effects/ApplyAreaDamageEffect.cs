using GrimSpace.Battle.Abilities;
using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.Spatial;
using GrimSpace.Battle.Units;
using GrimSpace.Battle.World;
using GrimSpace.Core.Actions;
using GrimSpace.Math;
using GrimSpace.Math.Grid;
using GrimSpace.Pilots;

namespace GrimSpace.Battle.Effects;

public sealed class ApplyAreaDamageEffect(
	EImpactCause cause,
	IReadOnlySet<Coord> cells,
	int damage,
	Coord origin,
	int shieldDamageBonus = 0,
	int shieldDamageBonusPercent = 0,
	int adjacentHitDamagePercent = 100,
	int damageBonusPercent = 0,
	bool destroySource = false) : IEffect<BattleWorld, ActorRuntime>
{
	public EImpactCause Cause => cause;
	public IReadOnlySet<Coord> Cells => cells;
	public int Damage => damage;
	public Coord Origin => origin;
	public int ShieldDamageBonus => shieldDamageBonus;
	public int ShieldDamageBonusPercent => shieldDamageBonusPercent;
	public int AdjacentHitDamagePercent => adjacentHitDamagePercent;
	public int DamageBonusPercent => damageBonusPercent;

	public ApplyAreaDamageEffect WithAdditionalDamage(int amount) =>
		new(
			cause,
			cells,
			damage + amount,
			origin,
			shieldDamageBonus,
			shieldDamageBonusPercent,
			adjacentHitDamagePercent,
			damageBonusPercent,
			destroySource);

	public ApplyAreaDamageEffect WithAdditionalShieldDamage(int amount) =>
		new(
			cause,
			cells,
			damage,
			origin,
			shieldDamageBonus + amount,
			shieldDamageBonusPercent,
			adjacentHitDamagePercent,
			damageBonusPercent,
			destroySource);

	public ApplyAreaDamageEffect WithShieldDamageBonusPercent(int percent) =>
		new(
			cause,
			cells,
			damage,
			origin,
			shieldDamageBonus,
			percent,
			adjacentHitDamagePercent,
			damageBonusPercent,
			destroySource);

	public ApplyAreaDamageEffect WithAdjacentHitDamagePercent(int percent) =>
		new(
			cause,
			cells,
			damage,
			origin,
			shieldDamageBonus,
			shieldDamageBonusPercent,
			percent,
			damageBonusPercent,
			destroySource);

	public ApplyAreaDamageEffect WithDamageBonusPercent(int percent) =>
		new(
			cause,
			cells,
			damage,
			origin,
			shieldDamageBonus,
			shieldDamageBonusPercent,
			adjacentHitDamagePercent,
			percent,
			destroySource);

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
			if (ApplyTo(
				unit.State,
				actorId,
				isAlliedSource,
				destroySource && unit.State.Id == actorId) is { } impact)
				records.Add(new Record<ImpactFacts>(impact));
		}

		return records;
	}

	public void Undo(BattleWorld world, ActorRuntime runtime, string actorId)
	{
		foreach (var (unitId, snapshot) in _snapshots)
			snapshot.Restore(world.StateOf(unitId));
	}

	private ImpactFacts? ApplyTo(
		State unit,
		string actorId,
		bool isAlliedSource,
		bool forceDestroy)
	{
		var face = BodyFrame.From(unit).HitFaceFrom(origin);
		var shieldBefore = Defense.EffectiveShield(unit, face);
		var hullBefore = unit.HullPoints;
		var appliedDamage = damage;
		if (damageBonusPercent != 0)
			appliedDamage = ScalePercent(appliedDamage, 100 + damageBonusPercent);
		if (origin.ManhattanDistanceTo(unit.Position) == 1 && adjacentHitDamagePercent != 100)
			appliedDamage = ScalePercent(appliedDamage, adjacentHitDamagePercent);

		var appliedShieldDamageBonus = shieldDamageBonus;
		if (shieldDamageBonusPercent > 0)
			appliedShieldDamageBonus += PortionOf(appliedDamage, shieldDamageBonusPercent);

		if (isAlliedSource && unit.InTheFrayActive)
		{
			var retained = PilotSkillPercents.InTheFray.AlliedDamageTakenPercent;
			var reducedShieldDamage = ScalePercent(appliedDamage + appliedShieldDamageBonus, retained);
			appliedDamage = ScalePercent(appliedDamage, retained);
			appliedShieldDamageBonus = reducedShieldDamage - appliedDamage;
		}

		if (damage > 0
			&& unit.DefensiveManeuversMpSpentLastTurn == 0
			&& RollsDeflection(unit))
		{
			var retained = PilotSkillPercents.DefensiveManeuvers.DeflectDamageTakenPercent;
			appliedDamage = ScalePercent(appliedDamage, retained);
			appliedShieldDamageBonus = ScalePercent(appliedShieldDamageBonus, retained);
		}

		if (appliedDamage > 0)
			Defense.ApplyDamage(unit, appliedDamage, face, appliedShieldDamageBonus);
		if (forceDestroy)
			unit.HullPoints = 0;

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

	private static int ScalePercent(int amount, int percent) =>
		(int)System.Math.Round(amount * percent / 100m, System.MidpointRounding.AwayFromZero);

	private static int PortionOf(int amount, int percent) => ScalePercent(amount, percent);

	private bool RollsDeflection(State unit)
	{
		var rollIndex = unit.DefensiveManeuversRollIndex++;
		var random = new StableRandom(
			new StableSeedMixer()
				.Add(unit.Id)
				.Add(rollIndex)
				.Add((int)cause)
				.Value);
		return random.NextDouble()
			< PilotSkillPercents.DefensiveManeuvers.DeflectChancePercent / 100.0;
	}
}
