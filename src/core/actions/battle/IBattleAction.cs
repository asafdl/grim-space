using GrimSpace.Core.Actions;
using GrimSpace.Core.Actions.Battle.Contexts;

namespace GrimSpace.Core.Actions.Battle;

/// <summary>
/// Battle execution capability: legality and effect resolution on <see cref="BattleBoard"/>.
/// </summary>
public interface IBattleAction
{
	bool IsLegal(BattleBoard board, BattlePlanContext context);

	IReadOnlyList<IEffect<BattleSlices>> Resolve(BattleBoard board, BattlePlanContext context);
}
