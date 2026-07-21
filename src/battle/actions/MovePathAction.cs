using GrimSpace.Battle.Effects;
using GrimSpace.Battle.Movement;
using GrimSpace.Battle.Slices;
using GrimSpace.Battle.Spatial;
using GrimSpace.Core.Actions;
using GrimSpace.Core.Actions.Battle;
using GrimSpace.Core.Engine;

namespace GrimSpace.Battle.Actions;

public sealed class MovePathAction(string ownerId, Option option, int? undoGroup = null) : IBattleAction
{
	public string OwnerId { get; } = ownerId;
	public int? UndoGroup { get; } = undoGroup;
	public Option Option { get; } = option;

	public bool IsLegal(BattleActionContext ctx)
	{
		if (ctx.TurnState.IsMovePathStarted)
			return false;

		var board = ctx.Board.Fork();
		var turnState = ctx.TurnState.Clone();
		var scratch = BattleActionContext.For(board, turnState, OwnerId);
		var actor = board.StateOf(OwnerId);
		var steps = MoveStepAction.BuildSteps(OwnerId, BodyFrame.From(actor), actor.Position, Option.Path);

		foreach (var step in steps)
		{
			if (!SimulationRunner<BattleActionContext, BattleSlices, IBattleAction>.TryStep(scratch, step))
				return false;
		}

		return true;
	}

	public IReadOnlyList<IEffect<BattleSlices>> Resolve(BattleActionContext ctx) =>
		[new MovePathEffect(OwnerId, Option.Path)];
}
