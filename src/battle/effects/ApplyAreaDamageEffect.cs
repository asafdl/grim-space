using GrimSpace.Battle.Abilities;
using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.Spatial;
using GrimSpace.Battle.Statuses;
using GrimSpace.Battle.Units;
using GrimSpace.Battle.World;
using GrimSpace.Core.Actions;
using GrimSpace.Math.Grid;

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
	bool destroySource = false,
	bool alliesTakeDamage = true,
	bool overdriveBonusIncluded = false,
	int hullBreachDamagePercent = 100) : IEffect<BattleWorld, ActorRuntime>
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
	public bool OverdriveBonusIncluded => overdriveBonusIncluded;
	public int HullBreachDamagePercent => hullBreachDamagePercent;

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
			destroySource,
			alliesTakeDamage,
			overdriveBonusIncluded,
			hullBreachDamagePercent);

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
			destroySource,
			alliesTakeDamage,
			overdriveBonusIncluded,
			hullBreachDamagePercent);

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
			destroySource,
			alliesTakeDamage,
			overdriveBonusIncluded,
			hullBreachDamagePercent);

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
			destroySource,
			alliesTakeDamage,
			overdriveBonusIncluded,
			hullBreachDamagePercent);

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
			destroySource,
			alliesTakeDamage,
			overdriveBonusIncluded,
			hullBreachDamagePercent);

	public ApplyAreaDamageEffect WithAddedDamageBonusPercent(int percent, bool includesOverdrive = false) =>
		new(
			cause,
			cells,
			damage,
			origin,
			shieldDamageBonus,
			shieldDamageBonusPercent,
			adjacentHitDamagePercent,
			adjacentHitDamageRangeCells,
			damageBonusPercent + percent,
			destroySource,
			alliesTakeDamage,
			overdriveBonusIncluded || includesOverdrive,
			hullBreachDamagePercent);

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
			&& BattleStatuses.HasOneWithTheVoid(detonator.State);
		foreach (var unit in UnitRegistry.For(world).All)
		{
			if (!unit.State.IsAlive || !cells.Contains(unit.State.Position))
				continue;

			if (!alliesTakeDamage)
			{
				var relation = detonator.RelationTo(unit);
				if (relation is EUnitRelation.Ally or EUnitRelation.Self)
					continue;
			}

			if (voidBombFriendlyImmunity
				&& detonator.RelationTo(unit) != EUnitRelation.Opponent)
			{
				if (destroySource && unit.State.Id == actorId)
				{
					_snapshots[unit.State.Id] = UnitCombatSnapshot.Capture(unit.State);
					unit.State.CurrentHullPoints = 0;
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
		var hullBefore = unit.CurrentHullPoints;
		var bonus = damageBonusPercent;
		if (!overdriveBonusIncluded)
			bonus += CrossingOverdriveBonusPercent(world.StateOf(actorId));
		var appliedDamage = bonus == 0
			? damage
			: ScalePercent(damage, 100 + bonus);
		if (origin.ManhattanDistanceTo(unit.Position) == adjacentHitDamageRangeCells
			&& adjacentHitDamagePercent != 100)
			appliedDamage = ScalePercent(appliedDamage, adjacentHitDamagePercent);

		if (hullBreachDamagePercent != 100 && appliedDamage > shieldBefore)
			appliedDamage = ScalePercent(appliedDamage, hullBreachDamagePercent);

		appliedDamage = BattleStatuses.RewriteIncomingDamage(unit.Statuses, appliedDamage);

		var appliedShieldDamageBonus = shieldDamageBonus;
		if (shieldDamageBonusPercent > 0)
			appliedShieldDamageBonus += PortionOf(appliedDamage, shieldDamageBonusPercent);

		var hit = BattleStatuses.RewriteIncomingHit(
			unit.Statuses,
			world,
			targetUnit,
			origin,
			cause,
			face,
			new IncomingHit(appliedDamage, appliedShieldDamageBonus));

		if (hit.Damage > 0)
			Defense.ApplyDamage(unit, hit.Damage, face, hit.ShieldDamageBonus);
		if (forceDestroy)
			unit.CurrentHullPoints = 0;

		var shieldAfter = Defense.EffectiveShield(unit, face);
		var shieldDamage = shieldBefore - shieldAfter;
		var hullDamage = hullBefore - unit.CurrentHullPoints;
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

	internal static int CrossingOverdriveBonusPercent(State actor)
	{
		if (!actor.HeatProfile.Participates
			|| !actor.Overdrive
			|| BattleStatuses.HasOverheat(actor))
			return 0;

		return actor.Loadout.OverdriveDamageBonusPercent;
	}

	/// <summary>
	/// Crossing-shot only: HeatChange sets Overdrive before damage Apply, but RewriteOutgoing already ran.
	/// </summary>
	internal static int ApplyOverdriveCrossingBonus(State actor, int appliedDamage)
	{
		if (appliedDamage <= 0)
			return appliedDamage;

		var bonus = CrossingOverdriveBonusPercent(actor);
		return bonus == 0 ? appliedDamage : ScalePercent(appliedDamage, 100 + bonus);
	}

	private static int ScalePercent(int amount, int percent) =>
		(int)System.Math.Round(amount * percent / 100m, System.MidpointRounding.AwayFromZero);

	private static int PortionOf(int amount, int percent) => ScalePercent(amount, percent);
}
