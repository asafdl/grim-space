using GrimSpace.Battle.World;
using GrimSpace.Battle.Runtime;
using GrimSpace.Core.Actions;

namespace GrimSpace.Battle.Effects;

public sealed class ConsumeSpinDiscountEffect : IEffect<BattleWorld, ActorRuntime>
{
	public void Apply(BattleWorld world, ActorRuntime runtime, string actorId) =>
		runtime.SpinDiscount = false;
}
