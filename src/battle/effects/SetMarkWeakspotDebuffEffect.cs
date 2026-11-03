using GrimSpace.Battle.Effects;
using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.Statuses;
using GrimSpace.Battle.Units;
using GrimSpace.Battle.World;
using GrimSpace.Core.Actions;
using GrimSpace.Math.Grid;

namespace GrimSpace.Battle.Effects;

public sealed class SetMarkWeakspotDebuffEffect(string targetId, ESpatialOrientation face, bool active)
	: IEffect<BattleWorld, ActorRuntime>
{
	private BattleStatus? _previous;

	public IReadOnlyList<IRecord> Apply(BattleWorld world, ActorRuntime runtime, string actorId)
	{
		var target = world.StateOf(targetId);
		var instanceId = BattleStatuses.BuffInstanceId("MarkWeakspot", targetId);
		_previous = target.Statuses.Find(status => status.InstanceId == instanceId)?.DeepCopy();
		BattleStatuses.RemoveInstance(target.Statuses, instanceId);
		if (active)
			target.Statuses.Add(BattleStatuses.CreateMarkWeakspot(targetId, face));
		return [];
	}

	public void Undo(BattleWorld world, ActorRuntime runtime, string actorId)
	{
		var target = world.StateOf(targetId);
		var instanceId = BattleStatuses.BuffInstanceId("MarkWeakspot", targetId);
		BattleStatuses.RemoveInstance(target.Statuses, instanceId);
		if (_previous is not null)
			target.Statuses.Add(_previous.DeepCopy());
		_previous = null;
	}
}
