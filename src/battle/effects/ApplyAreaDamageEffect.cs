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
	int adjacentHitDamageRangeCells = 1,
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
	public int AdjacentHitDamageRangeCells => adjacentHitDamageRangeCells;
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
			adjacentHitDamageRangeCells,
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
			adjacentHitDamageRangeCells,
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
			adjacentHitDamageRangeCells,
			damageBonusPercent,
			destroySource);

	public ApplyAreaDamageEffect WithAdjacentHitDamagePercent(int percent, int rangeCells = 1) =>
		new(
			cause,
			cells,
			damage,
			origin,
			shieldDamageBonus,
			shieldDamageBonusPercent,
			percent,
			rangeCells,
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
			adjacentHitDamageRangeCells,
			percent,
			destroySource);

	private Dictionary<string, UnitCombatSnapshot> _snapshots = [];

	public IReadOnlyList<IRecord> Apply(BattleWorld world, ActorRuntime runtime, string actorId)
	{
		_snapshots = new Dictionary<string, UnitCombatSnapshot>();
		var records = new List<IRecord>
		{
			new Record<AreaDamageFacts>(new AreaDamageFacts(actorId, cause, origin, cells)),
		};
		var detonator = UnitRegistry.For(world).UnitOf(actorId);
		var voidBombFriendlyImmunity = cause == EImpactCause.VoidBombBlast
			&& detonator.State.OneWithTheVoidActive;
		foreach (var unit in UnitRegistry.For(world).All)
		{
			if (!unit.State.IsAlive || !cells.Contains(unit.State.Position))
				continue;

			if (voidBombFriendlyImmunity
				&& detonator.RelationTo(unit) != EUnitRelation.Opponent)
			{
				if (destroySource && unit.State.Id == actorId)
				{
					_snapshots[unit.State.Id] = UnitCombatSnapshot.Capture(unit.State);
					unit.State.HullPoints = 0;
				}

				continue;
			}

			_snapshots[unit.State.Id] = UnitCombatSnapshot.Capture(unit.State);
			if (ApplyTo(
				world,
				unit,
				actorId,
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
		BattleWorld world,
		Unit targetUnit,
		string actorId,
		bool forceDestroy)
	{
		var unit = targetUnit.State;
		var face = BodyFrame.From(unit).HitFaceFrom(origin);
		var shieldBefore = Defense.EffectiveShield(unit, face);
		var hullBefore = unit.HullPoints;
		var appliedDamage = damage;
		if (damageBonusPercent != 0)
			appliedDamage = ScalePercent(appliedDamage, 100 + damageBonusPercent);
		if (origin.ManhattanDistanceTo(unit.Position) == adjacentHitDamageRangeCells
			&& adjacentHitDamagePercent != 100)
			appliedDamage = ScalePercent(appliedDamage, adjacentHitDamagePercent);

		var appliedShieldDamageBonus = shieldDamageBonus;
		if (shieldDamageBonusPercent > 0)
			appliedShieldDamageBonus += PortionOf(appliedDamage, shieldDamageBonusPercent);

		Defense.ApplyInTheFrayReduction(
			world,
			targetUnit,
			ref appliedDamage,
			ref appliedShieldDamageBonus);

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
