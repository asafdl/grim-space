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
	Coord origin) : IEffect<BattleWorld, ActorRuntime>
{
	private Dictionary<string, UnitCombatSnapshot> _snapshots = [];

	public IReadOnlyList<IRecord> Apply(BattleWorld world, ActorRuntime runtime, string actorId)
	{
		_snapshots = new Dictionary<string, UnitCombatSnapshot>();
		var impacts = new List<IRecord>();
		foreach (var unit in UnitRegistry.For(world).All)
		{
			if (!unit.State.IsAlive || !cells.Contains(unit.State.Position))
				continue;

			_snapshots[unit.State.Id] = UnitCombatSnapshot.Capture(unit.State);
			if (ApplyTo(unit.State, actorId) is { } impact)
				impacts.Add(new Record<ImpactFacts>(impact));
		}

		return impacts;
	}

	public void Undo(BattleWorld world, ActorRuntime runtime, string actorId)
	{
		foreach (var (unitId, snapshot) in _snapshots)
			snapshot.Restore(world.StateOf(unitId));
	}

	private ImpactFacts? ApplyTo(State unit, string actorId)
	{
		var face = BodyFrame.From(unit).HitFaceFrom(origin);
		var shieldBefore = unit.ShieldPoints[face];
		var hullBefore = unit.HullPoints;
		if (damage > 0)
			Defense.ApplyDamage(unit, damage, face);

		var shieldDamage = shieldBefore - unit.ShieldPoints[face];
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
