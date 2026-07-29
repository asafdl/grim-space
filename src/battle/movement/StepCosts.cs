using GrimSpace.Battle.Movement.Enums;

namespace GrimSpace.Battle.Movement;

public static class StepCosts
{
	public static int GetMoveStepApCost(ESpatialOrientation direction, MoveStepContext context)
	{
		var config = MomentumConfig.ForLevel(context.MomentumLevel);

		return direction switch
		{
			ESpatialOrientation.Forward => context.ForwardStepsInPath < config.FreeForwardSteps
				? 0
				: config.ForwardStepCost,
			ESpatialOrientation.Port or ESpatialOrientation.Starboard
				or ESpatialOrientation.Dorsal or ESpatialOrientation.Ventral => config.LateralCost,
			ESpatialOrientation.Retro => config.BrakeCost,
			_ => int.MaxValue,
		};
	}
}
