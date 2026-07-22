using GrimSpace.Battle.Board;
using GrimSpace.Battle.Runtime;
using GrimSpace.Core;
using GrimSpace.Core.Actions;

namespace GrimSpace.Battle.Effects;

public sealed class ClearTurnHazardsEffect : IEffect<BattleBoard, ActorSession>
{
	public void Apply(BattleBoard world, ActorSession runtime, string actorId)
	{
		var turnScoped = world.NonUnits.Values
			.Where(nonUnit => nonUnit.OwnerId != EntityIds.World)
			.Select(nonUnit => nonUnit.Id)
			.ToList();

		foreach (var id in turnScoped)
			world.MutableNonUnits.Remove(id);
	}
}
