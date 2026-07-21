using GrimSpace.Core.Actions;
using GrimSpace.Battle.Slices;
using GrimSpace.Battle.Effects;

namespace GrimSpace.Battle.Actions;

public sealed class EndOfPhaseAction(string ownerId, int? undoGroup = null) : IBattleAction
{
	public string OwnerId { get; } = ownerId;
	public int? UndoGroup { get; } = undoGroup;

	public bool IsLegal(BattleActionContext ctx) => true;

	public IReadOnlyList<IEffect<BattleSlices>> Resolve(BattleActionContext ctx)
	{
		if (ctx.TurnState.IsMovePathStarted)
			return [];

		return [new MomentumDecayEffect()];
	}
}
