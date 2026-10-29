using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.Units;
using GrimSpace.Battle.World;
using GrimSpace.Core.Actions;
using GrimSpace.Math.Grid;

namespace GrimSpace.Battle.Effects;

public sealed class ApplyAreaAllyHealEffect(
	EImpactCause cause,
	IReadOnlySet<Coord> cells,
	int healAmount,
	int healBonusPercent = 0) : IEffect<BattleWorld, ActorRuntime>
{
	public EImpactCause Cause => cause;
	public IReadOnlySet<Coord> Cells => cells;
	public int HealAmount => healAmount;
	public int HealBonusPercent => healBonusPercent;

	public ApplyAreaAllyHealEffect WithAdditionalHeal(int amount) =>
		new(cause, cells, healAmount + amount, healBonusPercent);

	public ApplyAreaAllyHealEffect WithHealBonusPercent(int percent) =>
		new(cause, cells, healAmount, percent);

	private Dictionary<string, UnitCombatSnapshot> _snapshots = [];

	public IReadOnlyList<IRecord> Apply(BattleWorld world, ActorRuntime runtime, string actorId)
	{
		_snapshots = new Dictionary<string, UnitCombatSnapshot>();
		var records = new List<IRecord>();
		var detonator = UnitRegistry.For(world).UnitOf(actorId);
		foreach (var unit in UnitRegistry.For(world).All)
		{
			if (!unit.State.IsAlive || !cells.Contains(unit.State.Position))
				continue;

			if (detonator.RelationTo(unit) != EUnitRelation.Ally)
				continue;

			_snapshots[unit.State.Id] = UnitCombatSnapshot.Capture(unit.State);
			if (ApplyTo(unit, actorId) is { } heal)
				records.Add(new Record<HealFacts>(heal));
		}

		return records;
	}

	public void Undo(BattleWorld world, ActorRuntime runtime, string actorId)
	{
		foreach (var (unitId, snapshot) in _snapshots)
			snapshot.Restore(world.StateOf(unitId));
	}

	private HealFacts? ApplyTo(Unit targetUnit, string actorId)
	{
		var appliedHeal = healAmount;
		if (healBonusPercent != 0)
			appliedHeal = ScalePercent(appliedHeal, 100 + healBonusPercent);

		if (appliedHeal <= 0)
			return null;

		var unit = targetUnit.State;
		var shieldFace = Defense.MostMissingShieldFace(unit);
		var shieldHealed = Defense.RestoreShieldsOnFace(unit, shieldFace, appliedHeal);
		var hullHealed = Defense.RestoreHull(unit, appliedHeal - shieldHealed);
		if (shieldHealed == 0 && hullHealed == 0)
			return null;

		return new HealFacts(
			actorId,
			unit.Id,
			cause,
			shieldFace,
			shieldHealed,
			hullHealed);
	}

	private static int ScalePercent(int amount, int percent) =>
		(int)System.Math.Round(amount * percent / 100m, System.MidpointRounding.AwayFromZero);
}
