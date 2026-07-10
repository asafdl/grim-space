using GrimSpace.Battle.Movement.Enums;
using GrimSpace.Battle.Units;

namespace GrimSpace.Battle.Actions;

public interface IActions
{
	int GetMoveStepApCost(EStepDirection direction, State unit);

	int GetApCost(IAction action, State unit);
	bool CanPerform(IAction action, State unit);
	void ApplyCost(IAction action, State unit);
}
