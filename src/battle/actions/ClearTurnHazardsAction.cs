using GrimSpace.Core;

namespace GrimSpace.Battle.Actions;

public sealed record ClearTurnHazardsAction : IBattleAction
{
	public string OwnerId { get; } = EntityIds.System;
	public int? UndoGroup { get; } = null;
	public IActionDef Definition => ClearTurnHazardsDef.Instance;
}
