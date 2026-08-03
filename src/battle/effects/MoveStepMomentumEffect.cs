using GrimSpace.Battle.World;
using GrimSpace.Battle.Movement;
using GrimSpace.Battle.Runtime;
using GrimSpace.Core.Actions;
using GrimSpace.Math.Grid;

namespace GrimSpace.Battle.Effects;

public sealed class MoveStepMomentumEffect(ESpatialOrientation direction) : IEffect<BattleWorld, ActorRuntime>
{
	public void Apply(BattleWorld world, ActorRuntime runtime, string actorId)
	{
		var path = runtime.ActivePath;
		if (path is null)
			return;

		var actor = world.StateOf(actorId);
		var buildup = MomentumConfig.ApplyMovementStep(
			path.MovementBuildup,
			direction,
			path.MoveStartMomentumLevel,
			runtime.MomentumGainedFromMovement);
		path.MovementBuildupLevel = buildup.Level;
		path.MovementBuildupForwardSteps = buildup.ForwardStepsTowardGain;
		actor.MomentumLevel = buildup.Level;
	}
}
