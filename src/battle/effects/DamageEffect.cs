using GrimSpace.Battle.Spatial;
using GrimSpace.Battle.Units;
using GrimSpace.Battle.World;
using GrimSpace.Battle.Runtime;
using GrimSpace.Core.Actions;
using GrimSpace.Math.Grid;

namespace GrimSpace.Battle.Effects;

public sealed class DamageEffect(string targetUnitId, int damage, Coord attackOrigin)
	: IEffect<BattleWorld, ActorRuntime>
{
	private UnitCombatSnapshot _snapshot;
	private bool _applied;

	public void Apply(BattleWorld world, ActorRuntime runtime, string actorId)
	{
		if (!UnitRegistry.For(world).TryGet(targetUnitId, out var unit))
			return;

		_snapshot = UnitCombatSnapshot.Capture(unit.State);
		_applied = true;

		var target = unit.State;
		var face = BodyFrame.From(target).HitFaceFrom(attackOrigin);
		Defense.ApplyDamage(target, damage, face);
	}

	public void Undo(BattleWorld world, ActorRuntime runtime, string actorId)
	{
		if (!_applied || !UnitRegistry.For(world).TryGet(targetUnitId, out var unit))
			return;

		_snapshot.Restore(unit.State);
	}
}
