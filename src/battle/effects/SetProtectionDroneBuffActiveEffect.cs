using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.World;
using GrimSpace.Core.Actions;

namespace GrimSpace.Battle.Effects;

public sealed class SetProtectionDroneBuffActiveEffect(string targetId, bool active)
	: IEffect<BattleWorld, ActorRuntime>
{
	private bool _previous;

	public IReadOnlyList<IRecord> Apply(BattleWorld world, ActorRuntime runtime, string actorId)
	{
		var target = world.StateOf(targetId);
		_previous = target.ProtectionDroneBuffActive;
		target.ProtectionDroneBuffActive = active;
		return [];
	}

	public void Undo(BattleWorld world, ActorRuntime runtime, string actorId) =>
		world.StateOf(targetId).ProtectionDroneBuffActive = _previous;
}
