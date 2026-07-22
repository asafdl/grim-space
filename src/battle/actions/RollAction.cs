using GrimSpace.Battle.Movement.Enums;

namespace GrimSpace.Battle.Actions;

public sealed record RollAction(
	string OwnerId,
	ERollDirection Direction,
	int? UndoGroup = null) : IBattleAction
{
	public IActionDef Definition => RollDef.Instance;
}
