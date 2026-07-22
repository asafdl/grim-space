using GrimSpace.Battle.Effects;
using GrimSpace.Battle.Slices;
using GrimSpace.Core.Actions;

namespace GrimSpace.Battle.Actions;

public sealed class ClearTurnHazardsDef : IActionDef
{
	public static ClearTurnHazardsDef Instance { get; } = new();

	public IEnumerable<IAction> Discover(BattleActionContext ctx, string ownerId) => [];

	public bool IsPossible(IAction action, BattleActionContext ctx) => true;

	public bool IsLegal(IAction action, BattleActionContext ctx) => true;

	public IReadOnlyList<IEffect<BattleSlices>> Resolve(IAction action, BattleActionContext ctx) =>
		[new ClearTurnHazardsEffect()];
}
