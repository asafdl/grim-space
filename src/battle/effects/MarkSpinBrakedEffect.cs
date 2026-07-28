using GrimSpace.Battle.World;
using GrimSpace.Battle.Runtime;
using GrimSpace.Core.Actions;

namespace GrimSpace.Battle.Effects;

public sealed class MarkSpinBrakedEffect : IEffect<BattleWorld, ActorRuntime>
{
	public void Apply(BattleWorld world, ActorRuntime runtime, string actorId)
	{
		runtime.SpinBraked = true;
		runtime.SpinDiscount = true;
		runtime.MinPathApCost = 0;
	}
}
