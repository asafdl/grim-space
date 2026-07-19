using GrimSpace.Battle.Ids;
using GrimSpace.Core.Actions;
using GrimSpace.Core.Actions.Battle.Contexts;
using GrimSpace.Core.Actions.Battle.Effects;

namespace GrimSpace.Core.Actions.Battle;

public sealed class ClearTurnHazardsAction : IAction
{
	public string OwnerId { get; } = EntityIds.System;

	public bool IsLegal(BattleBoard board, BattlePlanContext context) => true;

	public IReadOnlyList<IEffect<BattleSlices>> Resolve(BattleBoard board, BattlePlanContext context) =>
		[new ClearTurnHazardsEffect()];
}
