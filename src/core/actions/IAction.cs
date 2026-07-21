using GrimSpace.Core.Actions.Battle;
using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Slices;

namespace GrimSpace.Core.Actions;

public interface IAction
{
	string OwnerId { get; }

	int? UndoGroup { get; }

	bool IsLegal(BattleBoard board, BattlePlanContext context);

	IReadOnlyList<IEffect<BattleSlices>> Resolve(BattleBoard board, BattlePlanContext context);
}
