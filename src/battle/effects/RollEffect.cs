using GrimSpace.Battle.World;
using GrimSpace.Battle.Movement;
using GrimSpace.Battle.Movement.Enums;
using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.Units;
using GrimSpace.Core.Actions;

namespace GrimSpace.Battle.Effects;

public sealed class RollEffect(ERollDirection direction) : IEffect<BattleWorld, ActorRuntime>
{
	private OrientationSnapshot _previous;

	public IReadOnlyList<IRecord> Apply(BattleWorld world, ActorRuntime runtime, string actorId)
	{
		_previous = OrientationSnapshot.Capture(world.StateOf(actorId));
		Orientation.ApplyRoll(world.StateOf(actorId), direction);
		return [];
	}

	public void Undo(BattleWorld world, ActorRuntime runtime, string actorId) =>
		_previous.Restore(world.StateOf(actorId));
}
