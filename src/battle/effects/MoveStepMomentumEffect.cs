using GrimSpace.Battle.Movement;
using GrimSpace.Battle.Movement.Enums;
using GrimSpace.Battle.Turn;
using GrimSpace.Battle.Units;
using GrimSpace.Core.Actions;
using GrimSpace.Battle.Slices;

namespace GrimSpace.Battle.Effects;

public sealed class MoveStepMomentumEffect(EStepDirection direction) : IEffect<BattleSlices>
{
	public void Apply(State actor, TurnPhaseContext phaseContext)
	{
		var buildup = MomentumConfig.ApplyMovementStep(
			phaseContext.MovementBuildup,
			direction,
			phaseContext.MoveStartMomentumLevel,
			phaseContext.MomentumGainedFromMovement);
		phaseContext.MovementBuildupLevel = buildup.Level;
		phaseContext.MovementBuildupForwardSteps = buildup.ForwardStepsTowardGain;
		actor.MomentumLevel = buildup.Level;
	}

	void IEffect<BattleSlices>.Apply(BattleSlices slices) =>
		Apply(slices.Ap.Player, slices.PhaseContext);
}
