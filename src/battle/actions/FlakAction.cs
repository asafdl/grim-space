using GrimSpace.Battle.Weapons;

namespace GrimSpace.Battle.Actions;

public sealed record FlakAction(
	string OwnerId,
	EFlakMount Mount,
	int? UndoGroup = null) : IBattleAction
{
	public IActionDef Definition => FlakDef.For(Mount);
}
