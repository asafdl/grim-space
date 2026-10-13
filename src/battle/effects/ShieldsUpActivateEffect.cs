using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.Units;
using GrimSpace.Battle.World;
using GrimSpace.Core.Actions;

namespace GrimSpace.Battle.Effects;

public sealed class ShieldsUpActivateEffect : IEffect<BattleWorld, ActorRuntime>
{
	private bool _previousUsedThisTurn;

	public IReadOnlyList<IRecord> Apply(BattleWorld world, ActorRuntime runtime, string actorId)
	{
		var actor = world.StateOf(actorId);
		_previousUsedThisTurn = actor.ShieldsUpUsedThisTurn;
		actor.ShieldsUpUsedThisTurn = true;
		return [];
	}

	public void Undo(BattleWorld world, ActorRuntime runtime, string actorId) =>
		world.StateOf(actorId).ShieldsUpUsedThisTurn = _previousUsedThisTurn;
}
