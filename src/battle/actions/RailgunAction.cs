namespace GrimSpace.Battle.Actions;

public sealed record RailgunAction(
	string OwnerId,
	string TargetUnitId,
	int? UndoGroup = null) : IBattleAction
{
	public IActionDef Definition => RailgunDef.Instance;
}
