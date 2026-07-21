using GrimSpace.Battle.Movement;
using GrimSpace.Battle.Movement.Enums;
using GrimSpace.Battle.Spatial;
using GrimSpace.Battle.Units;
using GrimSpace.Battle.Weapons;
using GrimSpace.Core.Actions;
using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Board;
using GrimSpace.Math.Grid;

namespace GrimSpace.Battle.Actions;

/// <summary>
/// Rules-layer queries: which actions and cells are legal given board state and plan context.
/// </summary>
public static class LegalActions
{
	public static IEnumerable<IAction> EnumerateMovement(
		BattleBoard board,
		BattleActionContext context,
		string actorId)
	{
		foreach (var turn in Enum.GetValues<EHeadingTurn>())
		{
			var action = new HeadingTurnAction(actorId, turn);
			if (action.IsLegal(ForAction(context, action)))
				yield return action;
		}

		foreach (var direction in Enum.GetValues<ERollDirection>())
		{
			var action = new RollAction(actorId, direction);
			if (action.IsLegal(ForAction(context, action)))
				yield return action;
		}
	}

	public static IEnumerable<Option> EnumerateMovePaths(
		BattleBoard board,
		BattleActionContext context,
		string actorId)
	{
		if (context.TurnState.IsMovePathStarted)
			yield break;

		foreach (var option in GetMoveOptions(board, context, actorId))
			yield return option;
	}

	public static IReadOnlyList<Option> GetMoveOptions(
		BattleBoard board,
		BattleActionContext context,
		string actorId) =>
		MovePathFinder.Find(board, context.TurnState, actorId);

	public static HashSet<Coord> GetMissileCells(
		BattleBoard board,
		BattleActionContext context,
		string actorId,
		EMissileMount mount,
		int range)
	{
		var frame = BodyFrame.From(board.StateOf(actorId));
		var config = MissileMountConfig.For(mount).WithRange(range);
		var cells = MissileTargeting.GetValidCells(frame, config, board.Grid.IsInBounds);

		return cells
			.Where(cell =>
			{
				var action = new MissileAction(actorId, cell, mount, range);
				return action.IsLegal(ForAction(context, action));
			})
			.ToHashSet();
	}

	public static bool IsRailgunAvailable(
		BattleBoard board,
		BattleActionContext context,
		string actorId,
		string targetUnitId)
	{
		var action = new RailgunAction(actorId, targetUnitId);
		return action.IsLegal(ForAction(context, action));
	}

	public static bool IsFlakAvailable(BattleBoard board, BattleActionContext context, string actorId) =>
		Enum.GetValues<EFlakMount>().Any(mount =>
			new FlakAction(actorId, mount).IsLegal(ForAction(context, new FlakAction(actorId, mount))));

	public static HashSet<Coord> GetFlakBurstCells(
		BattleBoard board,
		BattleActionContext context,
		string actorId,
		EFlakMount mount)
	{
		var action = new FlakAction(actorId, mount);
		if (!action.IsLegal(ForAction(context, action)))
			return [];

		var frame = BodyFrame.From(board.StateOf(actorId));
		var config = FlakMountConfig.For(mount);
		return FlakTargeting.GetBurstCells(frame, config, board.Grid.IsInBounds);
	}

	private static BattleActionContext ForAction(BattleActionContext context, IAction action) =>
		BattleActionContext.For(context.Board, context.TurnState, action.OwnerId);
}
