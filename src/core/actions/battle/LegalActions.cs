using GrimSpace.Battle.Movement;
using GrimSpace.Battle.Movement.Enums;
using GrimSpace.Battle.Spatial;
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
	}

	public static IEnumerable<Option> EnumerateMovePaths(
		BattleBoard board,
		BattlePlanContext context,
		string actorId)
	{
		if (TurnPlanner.HasMoveSteps(context.PhaseActions))
			yield break;

		foreach (var option in GetMoveOptions(board, context, actorId))
			yield return option;
	}

	public static IReadOnlyList<Option> GetMoveOptions(
		BattleBoard board,
		BattlePlanContext context,
		string actorId) =>
		MovePathFinder.Find(board, context, actorId);

	public static HashSet<Coord> GetMissileCells(
		BattleBoard board,
		BattlePlanContext context,
		string actorId,
		EMissileMount mount,
		int range)
	{
		var frame = BodyFrame.From(board.StateOf(actorId));
		var config = MissileMountConfig.For(mount).WithRange(range);
		var cells = MissileTargeting.GetValidCells(frame, config, board.Grid.IsInBounds);

		return cells
			.Where(cell => new MissileAction(actorId, cell, mount, range).IsLegal(board, context))
			.ToHashSet();
	}

	public static bool IsRailgunAvailable(BattleBoard board, BattlePlanContext context, string actorId, string targetUnitId) =>
		new RailgunAction(actorId, targetUnitId).IsLegal(board, context);

	public static bool IsFlakAvailable(BattleBoard board, BattlePlanContext context, string actorId) =>
		Enum.GetValues<EFlakMount>().Any(mount => new FlakAction(actorId, mount).IsLegal(board, context));

	public static HashSet<Coord> GetFlakBurstCells(
		BattleBoard board,
		BattlePlanContext context,
		string actorId,
		EFlakMount mount)
	{
		if (!new FlakAction(actorId, mount).IsLegal(board, context))
			return [];

		var frame = BodyFrame.From(board.StateOf(actorId));
		var config = FlakMountConfig.For(mount);
		return FlakTargeting.GetBurstCells(frame, config, board.Grid.IsInBounds);
	}
}
