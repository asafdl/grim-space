using GrimSpace.Battle.World;
using GrimSpace.Battle.Runtime;
using GrimSpace.Core.Actions;

namespace GrimSpace.Battle.Effects;

public sealed class ConsumeSpinDiscountEffect : IEffect<BattleWorld, ActorRuntime>
{
	private bool _previous;

	public void Apply(BattleWorld world, ActorRuntime runtime, string actorId)
	{
		_previous = runtime.SpinDiscount;
		runtime.SpinDiscount = false;
	}

	public void Undo(BattleWorld world, ActorRuntime runtime, string actorId) =>
		runtime.SpinDiscount = _previous;
}
