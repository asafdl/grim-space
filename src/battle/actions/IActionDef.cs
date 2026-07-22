using GrimSpace.Battle.Slices;
using GrimSpace.Core.Actions;

namespace GrimSpace.Battle.Actions;

public interface IActionDef
{
	IEnumerable<IAction> Discover(BattleActionContext ctx, string ownerId);

	bool IsPossible(IAction action, BattleActionContext ctx);

	bool IsLegal(IAction action, BattleActionContext ctx);

	IReadOnlyList<IEffect<BattleSlices>> Resolve(IAction action, BattleActionContext ctx);
}
