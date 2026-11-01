using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.Statuses;
using GrimSpace.Battle.World;
using GrimSpace.Core.Actions;
using GrimSpace.Pilots;

namespace GrimSpace.Battle.Effects;

public sealed class SetProtectionDroneBuffActiveEffect(string targetId, bool active)
	: IEffect<BattleWorld, ActorRuntime>
{
	private BattleStatus? _previous;

	public IReadOnlyList<IRecord> Apply(BattleWorld world, ActorRuntime runtime, string actorId)
	{
		var target = world.StateOf(targetId);
		var instanceId = BattleStatuses.BuffInstanceId(nameof(EPilotCapabilityId.ProtectionDrone), targetId);
		_previous = target.Statuses.Find(status => status.InstanceId == instanceId)?.DeepCopy();
		BattleStatuses.RemoveInstance(target.Statuses, instanceId);
		if (active)
			target.Statuses.Add(BattleStatuses.CreateProtectionDrone(targetId));
		return [];
	}

	public void Undo(BattleWorld world, ActorRuntime runtime, string actorId)
	{
		var target = world.StateOf(targetId);
		BattleStatuses.RemoveInstance(
			target.Statuses,
			BattleStatuses.BuffInstanceId(nameof(EPilotCapabilityId.ProtectionDrone), targetId));
		if (_previous is not null)
			target.Statuses.Add(_previous);
	}
}
