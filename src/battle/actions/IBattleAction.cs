using GrimSpace.Battle.Slices;
using GrimSpace.Core.Actions;

namespace GrimSpace.Battle.Actions;

public interface IBattleAction : IAction<BattleActionContext, BattleSlices>
{
	IActionDef Definition { get; }

	bool IAction<BattleActionContext, BattleSlices>.IsLegal(BattleActionContext context) =>
		Definition.IsLegal(this, context);

	IReadOnlyList<IEffect<BattleSlices>> IAction<BattleActionContext, BattleSlices>.Resolve(
		BattleActionContext context) => Definition.Resolve(this, context);
}
