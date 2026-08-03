using GrimSpace.Battle.World;
using GrimSpace.Battle.Movement;
using GrimSpace.Battle.Movement.Enums;
using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.Units;
using GrimSpace.Core.Actions;

namespace GrimSpace.Battle.Effects;

public sealed class HeadingTurnEffect(EHeadingTurn turn) : IEffect<BattleWorld, ActorRuntime>
{
	private OrientationSnapshot _previous;

	public void Apply(BattleWorld world, ActorRuntime runtime, string actorId)
	{
		_previous = OrientationSnapshot.Capture(world.StateOf(actorId));
		Orientation.ApplyHeadingTurn(world.StateOf(actorId), turn);
	}

	public void Undo(BattleWorld world, ActorRuntime runtime, string actorId) =>
		_previous.Restore(world.StateOf(actorId));
}
