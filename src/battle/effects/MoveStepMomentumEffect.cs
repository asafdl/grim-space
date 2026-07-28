using GrimSpace.Battle.World;
using GrimSpace.Battle.Movement;
using GrimSpace.Battle.Movement.Enums;
using GrimSpace.Battle.Runtime;
using GrimSpace.Core.Actions;

namespace GrimSpace.Battle.Effects;

public sealed class MoveStepMomentumEffect(EStepDirection direction) : IEffect<BattleWorld, ActorRuntime>
{
	public void Apply(BattleWorld world, ActorRuntime runtime, string actorId)
	{
		var actor = world.StateOf(actorId);
		var buildup = MomentumConfig.ApplyMovementStep(
			runtime.MovementBuildup,
			direction,
			runtime.MoveStartMomentumLevel,
			runtime.MomentumGainedFromMovement);
		runtime.MovementBuildupLevel = buildup.Level;
		runtime.MovementBuildupForwardSteps = buildup.ForwardStepsTowardGain;
		actor.MomentumLevel = buildup.Level;
	}
}
