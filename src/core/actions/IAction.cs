using GrimSpace.Core.Actions.Battle;
using GrimSpace.Core.Actions.Battle.Contexts;

namespace GrimSpace.Core.Actions;

public interface IAction
{
	string OwnerId { get; }

	int? UndoGroup { get; }

	bool IsLegal(BattleBoard board, BattlePlanContext context);

	IReadOnlyList<IEffect<BattleSlices>> Resolve(BattleBoard board, BattlePlanContext context);
}
