using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Slices;
using GrimSpace.Battle.Spatial;
using GrimSpace.Core.Actions;
using GrimSpace.Core.Actions.Battle;
using GrimSpace.Core.Engine;
using GrimSpace.Math.Grid;

namespace GrimSpace.Battle.Effects;

public sealed class MovePathEffect(string ownerId, IReadOnlyList<Coord> path) : IEffect<BattleSlices>
{
	public void Apply(BattleSlices slices)
	{
		var board = slices.Board;
		var actor = board.StateOf(ownerId);
		var ctx = BattleActionContext.For(board, slices.TurnState, ownerId);
		var steps = MoveStepAction.BuildSteps(ownerId, BodyFrame.From(actor), actor.Position, path);

		foreach (var step in steps)
			SimulationRunner<BattleActionContext, BattleSlices, IBattleAction>.Step(ctx, step);
	}
}
