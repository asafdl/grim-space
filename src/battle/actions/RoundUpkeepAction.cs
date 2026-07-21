using GrimSpace.Core.Actions;
using GrimSpace.Battle.Slices;
using GrimSpace.Battle.Effects;

namespace GrimSpace.Battle.Actions;

public sealed class RoundUpkeepAction(string ownerId, int? undoGroup = null) : IBattleAction
{
	public string OwnerId { get; } = ownerId;
	public int? UndoGroup { get; } = undoGroup;

	public bool IsLegal(BattleActionContext ctx) => true;

	public IReadOnlyList<IEffect<BattleSlices>> Resolve(BattleActionContext ctx) =>
		[new RoundUpkeepEffect()];
}
