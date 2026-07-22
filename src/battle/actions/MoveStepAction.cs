using GrimSpace.Battle.Movement.Enums;

namespace GrimSpace.Battle.Actions;

public sealed record MoveStepAction(
	string OwnerId,
	EStepDirection Direction,
	int? UndoGroup = null) : IBattleAction
{
	public IActionDef Definition => MoveDef.Instance;
}
