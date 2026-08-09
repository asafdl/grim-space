using GrimSpace.Battle.World;
using GrimSpace.Battle.Movement;
using GrimSpace.Battle.Runtime;
using GrimSpace.Core.Actions;
using GrimSpace.Math.Grid;

namespace GrimSpace.Battle.Effects;

public sealed class MoveStepMomentumEffect(ESpatialOrientation direction) : IEffect<BattleWorld, ActorRuntime>
{
	private int _previousMomentum;
	private int _previousBuildupLevel;
	private int _previousBuildupForwardSteps;

	public IReadOnlyList<IRecord> Apply(BattleWorld world, ActorRuntime runtime, string actorId)
	{
		var path = runtime.ActivePath;
		if (path is null)
			return [];

		var actor = world.StateOf(actorId);
		_previousMomentum = actor.MomentumLevel;
		_previousBuildupLevel = path.MovementBuildupLevel;
		_previousBuildupForwardSteps = path.MovementBuildupForwardSteps;

		var buildup = MomentumConfig.ApplyMovementStep(
			path.MovementBuildup,
			direction,
			path.MoveStartMomentumLevel,
			runtime.MomentumGainedFromMovement);
		path.MovementBuildupLevel = buildup.Level;
		path.MovementBuildupForwardSteps = buildup.ForwardStepsTowardGain;
		actor.MomentumLevel = buildup.Level;
		return [];
	}

	public void Undo(BattleWorld world, ActorRuntime runtime, string actorId)
	{
		var path = runtime.ActivePath;
		if (path is null)
			return;

		world.StateOf(actorId).MomentumLevel = _previousMomentum;
		path.MovementBuildupLevel = _previousBuildupLevel;
		path.MovementBuildupForwardSteps = _previousBuildupForwardSteps;
	}
}
