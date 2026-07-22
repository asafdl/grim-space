namespace GrimSpace.Battle.Actions;

public sealed record ResolveHazardAction(
	string OwnerId,
	string HazardId,
	int? UndoGroup = null) : IBattleAction
{
	public IActionDef Definition => ResolveHazardDef.Instance;
}
