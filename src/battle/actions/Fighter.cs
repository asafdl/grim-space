using GrimSpace.Battle.Movement;
using GrimSpace.Battle.Movement.Enums;
using GrimSpace.Battle.Units;
using GrimSpace.Domain.Units;

namespace GrimSpace.Battle.Actions;

public sealed class Fighter : IActions
{
	public int GetMoveStepApCost(EStepDirection direction, State unit, MoveStepContext context)
	{
		var config = MomentumConfig.ForLevel(unit.MomentumLevel);

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

	public int GetApCost(IAction action, State unit) =>
		action is MoveAction move ? move.Option.ApCost : int.MaxValue;

	public bool CanPerform(IAction action, State unit) =>
		action is MoveAction move && unit.ActionPoints >= move.Option.ApCost;

	public void ApplyCost(IAction action, State unit) =>
		unit.ActionPoints -= GetApCost(action, unit);
}
