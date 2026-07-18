using GrimSpace.Battle.Movement;
using GrimSpace.Battle.Movement.Enums;
using GrimSpace.Battle.Spatial;
using GrimSpace.Battle.Units;
using GrimSpace.Core.Actions;
using GrimSpace.Core.Actions.Battle.Effects;
using GrimSpace.Core.Actions.Battle.Contexts;
using GrimSpace.Math.Grid;

namespace GrimSpace.Core.Actions.Battle;

public sealed class MoveAction(string ownerId, Option option) : IAction
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

	public IReadOnlyList<IEffect<BattleSlices>> Resolve(BattleBoard board, BattlePlanContext context)
	{
		var actor = board.StateOf(OwnerId);
		var effects = new List<IEffect<BattleSlices>>
		{
			new MoveEffect(Option),
			new ApChangeEffect(-Option.ApCost),
		};

		if (PathContainsRetro(actor, Option.Origin, Option.Path))
			effects.Add(new MarkSpinBrakedEffect());

		return effects;
	}

	private static bool PathContainsRetro(State unit, Coord origin, IReadOnlyList<Coord> path)
	{
		var frame = BodyFrame.From(unit);
		var position = origin;

		foreach (var next in path)
		{
			if (frame.DirectionOfStep(position, next) == EStepDirection.Retro)
				return true;

			position = next;
		}

		return false;
	}

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
