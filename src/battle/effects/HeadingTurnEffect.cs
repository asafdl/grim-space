using GrimSpace.Battle.World;
using GrimSpace.Battle.Movement;
using GrimSpace.Battle.Movement.Enums;
using GrimSpace.Battle.Runtime;
using GrimSpace.Core.Actions;

namespace GrimSpace.Battle.Effects;

public sealed class HeadingTurnEffect(EHeadingTurn turn) : IEffect<BattleWorld, ActorRuntime>
{
	public void Apply(BattleWorld world, ActorRuntime runtime, string actorId) =>
		Orientation.ApplyHeadingTurn(world.StateOf(actorId), turn);
}
