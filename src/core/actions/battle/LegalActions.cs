using GrimSpace.Math.Grid;
using GrimSpace.Battle.Movement;
using GrimSpace.Battle.Movement.Enums;
using GrimSpace.Battle.Weapons;

namespace GrimSpace.Core.Actions.Battle;

public static class LegalActions
{
	public static IReadOnlyList<Option> GetMoveOptions(BattleBoard board, BattlePlanContext context) =>
		board.PlayerUnit.Movement
			.GetPreviews(board.Player, board.Grid)
			.Where(option => new MoveAction(option).IsLegal(board, context))
			.ToList();

	public static HashSet<Coord> GetMissileCells(
		BattleBoard board,
		BattlePlanContext context,
		EMissileMount mount)
	{
		if (context.MissilesRemaining <= 0)
			return [];

		var config = MissileMountConfig.For(mount);
		var cells = MissileTargeting.GetValidCells(
			board.Player.Position,
			board.Player.ForwardDirection,
			board.Player.RightDirection,
			board.Player.UpDirection,
			config,
			board.Grid.IsInBounds);

		return cells
			.Where(cell => new MissileAction(cell, mount).IsLegal(board, context))
			.ToHashSet();
	}

	public static bool IsRailgunAvailable(BattleBoard board, BattlePlanContext context) =>
		new RailgunAction(board.Enemy.Id).IsLegal(board, context);

	public static bool IsRollAvailable(BattleBoard board, BattlePlanContext context) =>
		Enum.GetValues<ERollDirection>()
			.Any(direction => new RollAction(direction).IsLegal(board, context));

	public static bool IsHeadingTurnAvailable(BattleBoard board, BattlePlanContext context) =>
		Enum.GetValues<EHeadingTurn>()
			.Any(turn => new HeadingTurnAction(turn).IsLegal(board, context));
}
