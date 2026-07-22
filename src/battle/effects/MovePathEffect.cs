using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Slices;
using GrimSpace.Battle.Spatial;
using GrimSpace.Math.Grid;

namespace GrimSpace.Battle.Effects;

public sealed class MovePathEffect(string ownerId, IReadOnlyList<Coord> path) : IEffect<BattleSlices>
{
	public void Apply(BattleSlices slices)
	{
		var board = slices.Board;
		var actor = board.StateOf(ownerId);
		var ctx = BattleActionContext.For(board, slices.PhaseContext, ownerId);
		var steps = MoveDef.StepsFromPath(ownerId, BodyFrame.From(actor), actor.Position, path);

		foreach (var step in steps)
			BattleActionRunner.Apply(step, ctx);
	}
}
