using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.Statuses;
using GrimSpace.Battle.World;
using GrimSpace.Core.Actions;
using GrimSpace.Math.Grid;

namespace GrimSpace.Battle.Effects;

public sealed class SetMarkWeakspotDebuffEffect(string targetId, ESpatialOrientation face, bool active)
	: IEffect<BattleWorld, ActorRuntime>
{
	private MarkWeakspotDebuff? _previous;

	public IReadOnlyList<IRecord> Apply(BattleWorld world, ActorRuntime runtime, string actorId)
	{
		var target = world.StateOf(targetId);
		_previous = target.StatusOf<MarkWeakspotDebuff>()?.DeepCopy() as MarkWeakspotDebuff;
		target.RemoveStatus<MarkWeakspotDebuff>();
		if (active)
			target.Statuses.Add(new MarkWeakspotDebuff { Face = face });
		return [];
	}

	public void Undo(BattleWorld world, ActorRuntime runtime, string actorId)
	{
		var target = world.StateOf(targetId);
		target.RemoveStatus<MarkWeakspotDebuff>();
		if (_previous is not null)
			target.Statuses.Add(_previous.DeepCopy());
		_previous = null;
	}
}
