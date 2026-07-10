using GrimSpace.Battle.Movement;
using GrimSpace.Battle.Movement.Enums;
using GrimSpace.Battle.Units;

namespace GrimSpace.Battle.Actions;

public sealed class Fighter : IActions
{
	public int GetMoveStepApCost(EStepDirection direction, State unit) =>
		direction switch
		{
			EStepDirection.Forward => 1,
			EStepDirection.Dorsal or EStepDirection.Ventral or EStepDirection.Port or EStepDirection.Starboard => 1,
			EStepDirection.Retro => 2,
			_ => int.MaxValue,
		};

	public int GetApCost(IAction action, State unit) =>
		action is MoveAction move ? move.Option.ApCost : int.MaxValue;

	public bool CanPerform(IAction action, State unit) =>
		action is MoveAction move && unit.ActionPoints >= move.Option.ApCost;

	public void ApplyCost(IAction action, State unit) =>
		unit.ActionPoints -= GetApCost(action, unit);
}
