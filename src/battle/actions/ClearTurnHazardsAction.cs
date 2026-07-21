using GrimSpace.Core;
using GrimSpace.Core.Actions;
using GrimSpace.Battle.Slices;
using GrimSpace.Battle.Effects;

namespace GrimSpace.Battle.Actions;

public sealed class ClearTurnHazardsAction : IBattleAction
{
	public string OwnerId { get; } = EntityIds.System;
	public int? UndoGroup { get; } = null;

	public bool IsLegal(BattleActionContext ctx) => true;

	public IReadOnlyList<IEffect<BattleSlices>> Resolve(BattleActionContext ctx) =>
		[new ClearTurnHazardsEffect()];
}
