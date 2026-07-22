using GrimSpace.Battle.Board;
using GrimSpace.Battle.Movement;
using GrimSpace.Battle.Spatial;
using GrimSpace.Battle.Weapons;
using GrimSpace.Math.Grid;
using GrimSpace.Core.Actions;

namespace GrimSpace.Battle.Actions;

/// <summary>
/// Rules-layer queries built on action defs.
/// </summary>
public static class ActionQueries
{
	public static IEnumerable<IAction> EnumerateMovement(BattleActionContext ctx, string actorId)
	{
		foreach (var action in HeadingDef.Instance.Discover(ctx, actorId))
			yield return action;

		foreach (var action in RollDef.Instance.Discover(ctx, actorId))
			yield return action;
	}

	public static IEnumerable<Option> EnumerateMovePaths(
		BattleBoard board,
		BattleActionContext context,
		string actorId)
	{
		if (context.PhaseContext.IsMovePathStarted)
			yield break;

		foreach (var option in GetMoveOptions(board, context, actorId))
			yield return option;
	}

	public static IReadOnlyList<Option> GetMoveOptions(
		BattleBoard board,
		BattleActionContext context,
		string actorId) =>
		MovePathFinder.Find(board, context.PhaseContext, actorId);

	public static HashSet<Coord> GetMissileCells(
		BattleBoard board,
		BattleActionContext context,
		string actorId,
		EMissileMount mount,
		int range)
	{
		var def = MissileDef.For(mount, range);
		return def.Discover(context, actorId)
			.OfType<MissileAction>()
			.Select(missile => missile.Center)
			.ToHashSet();
	}

	public static bool IsRailgunAvailable(BattleActionContext context, string ownerId, string targetUnitId)
	{
		var action = new RailgunAction(ownerId, targetUnitId);
		return RailgunDef.Instance.IsLegal(action, context);
	}

	public static bool IsFlakAvailable(BattleActionContext context, string ownerId) =>
		Enum.GetValues<EFlakMount>().Any(mount =>
			FlakDef.For(mount).IsLegal(new FlakAction(ownerId, mount), context));

	public static HashSet<Coord> GetFlakBurstCells(
		BattleBoard board,
		BattleActionContext context,
		string ownerId,
		EFlakMount mount)
	{
		var action = new FlakAction(ownerId, mount);
		if (!FlakDef.For(mount).IsLegal(action, context))
			return [];

		var frame = BodyFrame.From(board.StateOf(ownerId));
		var config = FlakMountConfig.For(mount);
		return FlakTargeting.GetBurstCells(frame, config, board.Grid.IsInBounds);
	}
}
