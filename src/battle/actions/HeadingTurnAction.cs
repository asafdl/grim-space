using GrimSpace.Battle.Movement.Enums;

namespace GrimSpace.Battle.Actions;

public sealed record HeadingTurnAction(
	string OwnerId,
	EHeadingTurn Turn,
	int? UndoGroup = null) : IBattleAction
{
	public IActionDef Definition => HeadingDef.Instance;
}
