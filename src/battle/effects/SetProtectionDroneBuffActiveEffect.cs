using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.Statuses;
using GrimSpace.Battle.World;
using GrimSpace.Core.Actions;

namespace GrimSpace.Battle.Effects;

public sealed class SetProtectionDroneBuffActiveEffect(string targetId, bool active)
	: IEffect<BattleWorld, ActorRuntime>
{
	private ProtectionDroneBuff? _previous;

	public IReadOnlyList<IRecord> Apply(BattleWorld world, ActorRuntime runtime, string actorId)
	{
		var target = world.StateOf(targetId);
		_previous = target.StatusOf<ProtectionDroneBuff>()?.DeepCopy() as ProtectionDroneBuff;
		if (!active && _previous is null)
			return [];

		target.RemoveStatus<ProtectionDroneBuff>();
		ProtectionDroneBuff? status = active ? new ProtectionDroneBuff() : null;
		if (status is not null)
			target.Statuses.Add(status);
		return [new Record<ProtectionDroneChangedFacts>(new(actorId, targetId, status?.DeepCopy()))];
	}

	public void Undo(BattleWorld world, ActorRuntime runtime, string actorId)
	{
		var target = world.StateOf(targetId);
		target.RemoveStatus<ProtectionDroneBuff>();
		if (_previous is not null)
			target.Statuses.Add(_previous);
	}
}
