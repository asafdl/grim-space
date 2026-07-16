using GrimSpace.Battle.Units;
using GrimSpace.Math.Grid;
using GrimSpace.Battle.Movement;
using GrimSpace.Battle.Movement.Enums;
using GrimSpace.Battle.Weapons;
using BoundedGrid = GrimSpace.Math.Grid.Grid;

namespace GrimSpace.Core.Actions.Battle;

/// <summary>
/// Rules-layer queries: which actions and cells are legal given board state and plan context.
/// </summary>
public static class LegalActions
{
	public static IEnumerable<IBattleAction> EnumerateMovement(
		Unit actor,
		Unit opponent,
		BoundedGrid grid,
		IReadOnlyList<IBattleAction> plan,
		GridBasis startFacing,
		IReadOnlySet<Coord> blockedCells)
	{
		var context = new BattlePlanContext(plan, startFacing);
		var board = PlanSimulator.BuildBoard(actor, opponent, grid, plan, blockedCells);

		foreach (var turn in Enum.GetValues<EHeadingTurn>())
		{
			var action = new HeadingTurnAction(turn);
			if (action.IsLegal(board, context))
				yield return action;
		}

		foreach (var direction in Enum.GetValues<ERollDirection>())
		{
			var action = new RollAction(direction);
			if (action.IsLegal(board, context))
				yield return action;
		}

		var moveBoard = PlanSimulator.BuildBoard(actor, opponent, grid, plan, blockedCells, excludeMoves: true);
		foreach (var option in GetMoveOptions(moveBoard, context))
			yield return new MoveAction(option);
	}

	public static IReadOnlyList<Option> GetMoveOptions(BattleBoard board, BattlePlanContext context)
	{
		var blocked = new HashSet<Coord>(board.BlockedCells) { board.Enemy.Position };
		return board.PlayerUnit.Movement
			.GetMoveOptions(board.Player, board.Grid, blocked)
			.Where(option => new MoveAction(option).IsLegal(board, context))
			.ToList();
	}

	public static HashSet<Coord> GetMissileCells(
		BattleBoard board,
		BattlePlanContext context,
		EMissileMount mount,
		int range)
	{
		if (context.MissilesRemaining <= 0)
			return [];

		var config = MissileMountConfig.For(mount).WithRange(range);
		var cells = MissileTargeting.GetValidCells(
			board.Player.Position,
			board.Player.ForwardDirection,
			board.Player.RightDirection,
			board.Player.UpDirection,
			config,
			board.Grid.IsInBounds);

		return cells
			.Where(cell => new MissileAction(cell, mount, range).IsLegal(board, context))
			.ToHashSet();
	}

	public static bool IsRailgunAvailable(BattleBoard board, BattlePlanContext context) =>
		new RailgunAction(board.Enemy.Id).IsLegal(board, context);
}
