using GrimSpace.Battle.Board;
using GrimSpace.Battle.Movement;
using GrimSpace.Battle.Movement.Enums;
using GrimSpace.Battle.Runtime;
using GrimSpace.Core.Actions;

namespace GrimSpace.Battle.Effects;

public sealed class MoveStepMomentumEffect(EStepDirection direction) : IEffect<BattleBoard, ActorSession>
{
	public void Apply(BattleBoard world, ActorSession runtime, string actorId)
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
