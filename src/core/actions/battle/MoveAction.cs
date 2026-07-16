using GrimSpace.Battle.Movement;
using GrimSpace.Core.Actions;
using GrimSpace.Core.Actions.Battle.Effects;

using GrimSpace.Core.Actions.Battle.Contexts;
using GrimSpace.Math.Grid;

namespace GrimSpace.Core.Actions.Battle;

public sealed class MoveAction(Option option) : IBattleAction
{
	public Option Option { get; } = option;

	public EnqueuePolicy EnqueuePolicy => EnqueuePolicy.ReplaceSameType;

	public bool IsLegal(BattleBoard board, BattlePlanContext context) =>
		StepCosts.CanAffordMove(board.Player, Option)
		&& board.PlayerUnit.Movement.CanMove(board.Player, Option)
		&& !PathCrossesBlockedCells(board.BlockedCells, Option);

	public IReadOnlyList<IEffect<BattleSlices>> Resolve(BattleBoard board) =>
	[
		new MoveEffect(Option),
		new ApChangeEffect(-Option.ApCost),
	];

	private static bool PathCrossesBlockedCells(IReadOnlySet<Coord> blockedCells, Option option)
	{
		foreach (var cell in option.Path)
		{
			if (blockedCells.Contains(cell))
				return true;
		}

		return false;
	}
}
