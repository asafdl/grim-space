namespace GrimSpace.Battle.Actions;

public sealed record RoundUpkeepAction(
	string OwnerId,
	int? UndoGroup = null) : IBattleAction
{
	public IActionDef Definition => RoundUpkeepDef.Instance;
}
