using GrimSpace.Battle.World;
using GrimSpace.Battle.Runtime;
using GrimSpace.Core.Actions;

namespace GrimSpace.Battle.Effects;

public sealed class AddYawQuartersEffect(int delta) : IEffect<BattleWorld, ActorRuntime>
{
	public IReadOnlyList<IRecord> Apply(BattleWorld world, ActorRuntime runtime, string actorId)
	{
		runtime.RawYawQuarters += delta;
		return [];
	}

	public void Undo(BattleWorld world, ActorRuntime runtime, string actorId) =>
		runtime.RawYawQuarters -= delta;
}
