using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.Statuses;
using GrimSpace.Battle.Units;
using GrimSpace.Battle.World;
using GrimSpace.Core.Actions;
using GrimSpace.Units.Loadouts.Defenses;

namespace GrimSpace.Battle.Effects;

public sealed class ReleaseShieldBraceEffect : IEffect<BattleWorld, ActorRuntime>
{
	private BattleStatus? _previousStatus;
	private FaceShieldPoints? _previousShieldPoints;

	public IReadOnlyList<IRecord> Apply(BattleWorld world, ActorRuntime runtime, string actorId)
	{
		var actor = world.StateOf(actorId);
		_previousStatus = ShieldBrace.TryStatus(actor)?.DeepCopy();
		_previousShieldPoints = actor.ShieldPoints.Clone();
		ShieldBrace.Release(actor);
		return [];
	}

	public void Undo(BattleWorld world, ActorRuntime runtime, string actorId)
	{
		var actor = world.StateOf(actorId);
		actor.ShieldPoints = _previousShieldPoints!.Clone();
		ShieldBrace.Restore(actor, _previousStatus?.DeepCopy());
	}
}
