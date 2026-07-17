using GrimSpace.Battle.Movement;
using GrimSpace.Battle.Movement.Enums;
using GrimSpace.Battle.Units;
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

	public IReadOnlyList<IEffect<BattleSlices>> Resolve(BattleBoard board, BattlePlanContext context)
	{
		var actor = board.StateOf(OwnerId);
		if (PathContainsRetro(actor, Option.Origin, Option.Path))
			context.Tags.Spin.MarkBrakedFromRetro();

		return
		[
			new MoveEffect(Option),
			new ApChangeEffect(-Option.ApCost),
		];
	}

	private static bool PathContainsRetro(State unit, Coord origin, IReadOnlyList<Coord> path)
	{
		var position = origin;

		foreach (var next in path)
		{
			if (DirectionOfStep(unit, position, next) == EStepDirection.Retro)
				return true;

			position = next;
		}

		return false;
	}

	private static EStepDirection? DirectionOfStep(State unit, Coord from, Coord to)
	{
		var delta = to - from;

		if (delta == unit.ForwardDirection)
			return EStepDirection.Forward;

		if (delta == Coord.Zero - unit.ForwardDirection)
			return EStepDirection.Retro;

		if (delta == unit.UpDirection)
			return EStepDirection.Dorsal;

		if (delta == Coord.Zero - unit.UpDirection)
			return EStepDirection.Ventral;

		if (delta == unit.RightDirection)
			return EStepDirection.Starboard;

		if (delta == Coord.Zero - unit.RightDirection)
			return EStepDirection.Port;

		return null;
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
