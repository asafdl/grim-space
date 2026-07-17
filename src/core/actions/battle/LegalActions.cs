using GrimSpace.Battle.Movement;
using GrimSpace.Battle.Movement.Enums;
using GrimSpace.Battle.Units;
using GrimSpace.Battle.Weapons;
using GrimSpace.Core.Actions;
using GrimSpace.Math.Grid;

namespace GrimSpace.Core.Actions.Battle;

/// <summary>
/// Rules-layer queries: which actions and cells are legal given board state and plan context.
/// </summary>
public static class LegalActions
{
	public static IEnumerable<IAction> EnumerateMovement(
		BattleBoard board,
		BattlePlanContext context,
		string actorId)
	{
		foreach (var turn in Enum.GetValues<EHeadingTurn>())
		{
			var action = new HeadingTurnAction(actorId, turn);
			if (action.IsLegal(board, context))
				yield return action;
		}

		foreach (var direction in Enum.GetValues<ERollDirection>())
		{
			var action = new RollAction(actorId, direction);
			if (action.IsLegal(board, context))
				yield return action;
		}

		if (context.QueuedActions.Any(action => action is MoveAction))
			yield break;

		foreach (var option in GetMoveOptions(board, context, actorId))
			yield return new MoveAction(actorId, option);
	}

	public static IReadOnlyList<Option> GetMoveOptions(BattleBoard board, BattlePlanContext context, string actorId)
	{
		var actor = board.StateOf(actorId);
		var blocked = board.BlockedFor(actorId);
		return board.UnitOf(actorId).Movement
			.GetMoveOptions(actor, board.Grid, blocked)
			.Where(option => new MoveAction(actorId, option).IsLegal(board, context))
			.ToList();
	}

	public static HashSet<Coord> GetMissileCells(
		BattleBoard board,
		BattlePlanContext context,
		string actorId,
		EMissileMount mount,
		int range)
	{
		var actor = board.StateOf(actorId);
		var config = MissileMountConfig.For(mount).WithRange(range);
		var cells = MissileTargeting.GetValidCells(
			actor.Position,
			actor.ForwardDirection,
			actor.RightDirection,
			actor.UpDirection,
			config,
			board.Grid.IsInBounds);

		return cells
			.Where(cell => new MissileAction(actorId, cell, mount, range).IsLegal(board, context))
			.ToHashSet();
	}

	public static bool IsRailgunAvailable(BattleBoard board, BattlePlanContext context, string actorId, string targetUnitId) =>
		new RailgunAction(actorId, targetUnitId).IsLegal(board, context);
}
