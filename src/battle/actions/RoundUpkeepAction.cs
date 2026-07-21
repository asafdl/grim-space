using GrimSpace.Core.Actions;
using GrimSpace.Core.Actions.Battle;
using GrimSpace.Battle.Slices;
using GrimSpace.Battle.Effects;

namespace GrimSpace.Battle.Actions;

public sealed class RoundUpkeepAction(string ownerId, int? undoGroup = null) : IBattleAction
{
	public string OwnerId { get; } = ownerId;
	public int? UndoGroup { get; } = undoGroup;

	public bool IsLegal(BattleBoard board, TurnState state, IEnumerable<IAction> applied) => true;

	public IReadOnlyList<IEffect<BattleSlices>> Resolve(BattleBoard board, TurnState state, IEnumerable<IAction> applied) =>
		[new RoundUpkeepEffect()];
}
