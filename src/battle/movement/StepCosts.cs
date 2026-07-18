using GrimSpace.Battle.Movement.Enums;

namespace GrimSpace.Battle.Movement;

public static class StepCosts
{
	public static int GetMoveStepApCost(EStepDirection direction, MoveStepContext context)
	{
		var config = MomentumConfig.ForLevel(context.MomentumLevel);

		return direction switch
		{
			EStepDirection.Forward => context.ForwardStepsInPath < config.FreeForwardSteps
				? 0
				: config.ForwardStepCost,
			EStepDirection.Port or EStepDirection.Starboard
				or EStepDirection.Dorsal or EStepDirection.Ventral => config.LateralCost,
			EStepDirection.Retro => config.BrakeCost,
			_ => int.MaxValue,
		};
	}
}
