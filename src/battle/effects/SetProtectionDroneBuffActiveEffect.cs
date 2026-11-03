using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.Statuses;
using GrimSpace.Battle.World;
using GrimSpace.Core.Actions;
using GrimSpace.Units.Loadouts.Abilities;

namespace GrimSpace.Battle.Effects;

public sealed class SetProtectionDroneBuffActiveEffect(string targetId, bool active)
	: IEffect<BattleWorld, ActorRuntime>
{
	private BattleStatus? _previous;

	public IReadOnlyList<IRecord> Apply(BattleWorld world, ActorRuntime runtime, string actorId)
	{
		var target = world.StateOf(targetId);
		var instanceId = BattleStatuses.BuffInstanceId(nameof(EAbilityKind.ProtectionDrone), targetId);
		_previous = target.Statuses.Find(status => status.InstanceId == instanceId)?.DeepCopy();
		if (!active && _previous is null)
			return [];

		BattleStatuses.RemoveInstance(target.Statuses, instanceId);
		var status = active ? BattleStatuses.CreateProtectionDrone(targetId) : null;
		if (status is not null)
			target.Statuses.Add(status);
		return [new Record<ProtectionDroneChangedFacts>(new(actorId, targetId, status?.DeepCopy()))];
	}

	public void Undo(BattleWorld world, ActorRuntime runtime, string actorId)
	{
		var target = world.StateOf(targetId);
		BattleStatuses.RemoveInstance(
			target.Statuses,
			BattleStatuses.BuffInstanceId(nameof(EAbilityKind.ProtectionDrone), targetId));
		if (_previous is not null)
			target.Statuses.Add(_previous);
	}
}
