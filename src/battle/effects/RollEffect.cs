using GrimSpace.Battle.World;
using GrimSpace.Battle.Movement;
using GrimSpace.Battle.Movement.Enums;
using GrimSpace.Battle.Runtime;
using GrimSpace.Core.Actions;

namespace GrimSpace.Battle.Effects;

public sealed class RollEffect(ERollDirection direction) : IEffect<BattleWorld, ActorRuntime>
{
	public void Apply(BattleWorld world, ActorRuntime runtime, string actorId) =>
		Orientation.ApplyRoll(world.StateOf(actorId), direction);
}
