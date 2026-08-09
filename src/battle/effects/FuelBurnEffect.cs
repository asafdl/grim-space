using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.World;
using GrimSpace.Core.Actions;

namespace GrimSpace.Battle.Effects;

public sealed class FuelBurnEffect : IEffect<BattleWorld, ActorRuntime>
{
	private int _previous;

	public IReadOnlyList<IRecord> Apply(BattleWorld world, ActorRuntime runtime, string actorId)
	{
		var actor = world.StateOf(actorId);
		_previous = actor.FuelRemaining;
		actor.FuelRemaining = System.Math.Max(actor.FuelRemaining - 1, 0);
		return [];
	}

	public void Undo(BattleWorld world, ActorRuntime runtime, string actorId) =>
		world.StateOf(actorId).FuelRemaining = _previous;
}
