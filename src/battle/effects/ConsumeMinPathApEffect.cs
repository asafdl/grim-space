using GrimSpace.Battle.Board;
using GrimSpace.Battle.Runtime;
using GrimSpace.Core.Actions;

namespace GrimSpace.Battle.Effects;

public sealed class ConsumeMinPathApEffect(int stepApCost) : IEffect<BattleBoard, ActorSession>
{
	public void Apply(BattleBoard world, ActorSession runtime, string actorId)
	{
		runtime.MinPathApCost = System.Math.Max(0, runtime.MinPathApCost - stepApCost);
		if (stepApCost > 0)
			runtime.PathApSpent += stepApCost;
	}
}
