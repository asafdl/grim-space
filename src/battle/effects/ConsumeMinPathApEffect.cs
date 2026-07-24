using GrimSpace.Battle.Board;
using GrimSpace.Battle.Runtime;
using GrimSpace.Core.Actions;

namespace GrimSpace.Battle.Effects;

public sealed class ConsumeMinPathApEffect(int stepApCost) : IEffect<BattleBoard, ActorSession>
{
	public void Apply(BattleBoard world, ActorSession runtime, string actorId)
	{
		var minPathConsumption = System.Math.Max(1, stepApCost);
		runtime.MinPathApCost = System.Math.Max(0, runtime.MinPathApCost - minPathConsumption);
		if (stepApCost > 0)
			runtime.PathApSpent += stepApCost;
	}
}
