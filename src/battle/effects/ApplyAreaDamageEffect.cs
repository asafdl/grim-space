using GrimSpace.Battle.Abilities;
using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.Spatial;
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
	int shieldDamageBonus = 0) : IEffect<BattleWorld, ActorRuntime>
{
	public EImpactCause Cause => cause;
	public IReadOnlySet<Coord> Cells => cells;
	public int Damage => damage;
	public Coord Origin => origin;
	public int ShieldDamageBonus => shieldDamageBonus;

	public ApplyAreaDamageEffect WithAdditionalShieldDamage(int amount) =>
		new(cause, cells, damage, origin, shieldDamageBonus + amount);

	private Dictionary<string, UnitCombatSnapshot> _snapshots = [];

	public IReadOnlyList<IRecord> Apply(BattleWorld world, ActorRuntime runtime, string actorId)
	{
		_snapshots = new Dictionary<string, UnitCombatSnapshot>();
		var records = new List<IRecord>
		{
			new Record<AreaDamageFacts>(new AreaDamageFacts(actorId, cause, origin, cells)),
		};
		foreach (var unit in UnitRegistry.For(world).All)
		{
			if (!unit.State.IsAlive || !cells.Contains(unit.State.Position))
				continue;

			_snapshots[unit.State.Id] = UnitCombatSnapshot.Capture(unit.State);
			if (ApplyTo(unit.State, actorId) is { } impact)
				records.Add(new Record<ImpactFacts>(impact));
		}

		return records;
	}

	public void Undo(BattleWorld world, ActorRuntime runtime, string actorId)
	{
		foreach (var (unitId, snapshot) in _snapshots)
			snapshot.Restore(world.StateOf(unitId));
	}

	private ImpactFacts? ApplyTo(State unit, string actorId)
	{
		var face = BodyFrame.From(unit).HitFaceFrom(origin);
		var shieldBefore = Defense.EffectiveShield(unit, face);
		var hullBefore = unit.HullPoints;
		if (damage > 0)
			Defense.ApplyDamage(unit, damage, face, shieldDamageBonus);

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
}
