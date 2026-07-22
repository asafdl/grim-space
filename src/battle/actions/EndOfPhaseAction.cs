namespace GrimSpace.Battle.Actions;

public sealed record EndOfPhaseAction(
	string OwnerId,
	int? UndoGroup = null) : IBattleAction
{
	public IActionDef Definition => EndOfPhaseDef.Instance;
}
