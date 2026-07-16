using GrimSpace.Battle.Movement;
using GrimSpace.Core.Actions;
using GrimSpace.Core.Actions.Battle.Effects;
using GrimSpace.Core.Actions.Battle.Contexts;
using GrimSpace.Math.Grid;

namespace GrimSpace.Core.Actions.Battle;

public sealed class MoveAction(string ownerId, Option option) : IAction, IBattleAction
{
	public string OwnerId { get; } = ownerId;
	public Option Option { get; } = option;

	public bool IsLegal(BattleBoard board, BattlePlanContext context)
	{
		var actor = board.StateOf(OwnerId);
		return StepCosts.CanAffordMove(actor, Option)
			&& board.UnitOf(OwnerId).Movement.CanMove(actor, Option)
			&& !PathCrossesBlockedCells(board.BlockedFor(OwnerId), Option);
	}

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
