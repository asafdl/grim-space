using GrimSpace.Core;
using GrimSpace.Core.Actions;

namespace GrimSpace.Battle.Actions;

public sealed record ClearTurnHazardsAction : IAction
{
	public string OwnerId { get; } = EntityIds.System;
	public int? UndoGroup { get; } = null;
}
