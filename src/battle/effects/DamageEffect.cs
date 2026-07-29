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
	public void Apply(BattleWorld world, ActorRuntime runtime, string actorId)
	{
		if (!world.Units.TryGetValue(targetUnitId, out var unit))
			return;

		var target = unit.State;
		var face = BodyFrame.From(target).HitFaceFrom(attackOrigin);
		Defense.ApplyDamage(target, damage, face);
	}
}
