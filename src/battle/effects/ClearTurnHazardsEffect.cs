using GrimSpace.Battle.World;
using GrimSpace.Battle.Runtime;
using GrimSpace.Core;
using GrimSpace.Core.Actions;

namespace GrimSpace.Battle.Effects;

public sealed class ClearTurnHazardsEffect : IEffect<BattleWorld, ActorRuntime>
{
	public void Apply(BattleWorld world, ActorRuntime runtime, string actorId)
	{
		var turnScoped = world.NonUnits.Values
			.Where(nonUnit => nonUnit.ActorId != EntityIds.World)
			.Select(nonUnit => nonUnit.Id)
			.ToList();

		foreach (var id in turnScoped)
			world.MutableNonUnits.Remove(id);
	}
}
