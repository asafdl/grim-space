using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.World;
using GrimSpace.Core.Actions;

namespace GrimSpace.Battle.Effects;

public sealed class RefreshActionPointsEffect(int amount) : IEffect<BattleWorld, ActorRuntime>
{
	private int _granted;

	public IReadOnlyList<IRecord> Apply(BattleWorld world, ActorRuntime runtime, string actorId)
	{
		var actor = world.StateOf(actorId);
		var headroom = actor.Maneuverability.BaseActionPoints - actor.CurrentActionPoints;
		_granted = System.Math.Min(amount, System.Math.Max(0, headroom));
		actor.CurrentActionPoints += _granted;
		return [];
	}

	public void Undo(BattleWorld world, ActorRuntime runtime, string actorId) =>
		world.StateOf(actorId).CurrentActionPoints -= _granted;
}
