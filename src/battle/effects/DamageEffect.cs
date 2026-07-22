using GrimSpace.Battle.Board;
using GrimSpace.Battle.Runtime;
using GrimSpace.Core.Actions;

namespace GrimSpace.Battle.Effects;

public sealed class DamageEffect(string targetUnitId, int damage) : IEffect<BattleBoard, ActorSession>
{
	public void Apply(BattleBoard world, ActorSession runtime, string actorId)
	{
		if (!world.Units.TryGetValue(targetUnitId, out var unit))
			return;

		var target = unit.State;
		target.Hp = System.Math.Max(target.Hp - damage, 0);
	}
}
