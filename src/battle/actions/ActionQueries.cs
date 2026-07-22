using GrimSpace.Battle.Board;
using GrimSpace.Battle.Movement;
using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.Spatial;
using GrimSpace.Battle.Weapons;
using GrimSpace.Core.Actions;
using GrimSpace.Math.Grid;

namespace GrimSpace.Battle.Actions;

/// <summary>
/// Rules-layer queries built on action defs.
/// </summary>
public static class ActionQueries
{
	public static IEnumerable<IAction> EnumerateMovement(
		BattleBoard world,
		ActorSession runtime,
		string actorId)
	{
		foreach (var action in HeadingDef.Instance.Discover(world, runtime, actorId))
			yield return action;

		foreach (var action in RollDef.Instance.Discover(world, runtime, actorId))
			yield return action;
	}

	public static IEnumerable<Option> EnumerateMovePaths(
		BattleBoard board,
		ActorSession runtime,
		string actorId)
	{
		if (runtime.IsMovePathStarted)
			yield break;

		foreach (var option in GetMoveOptions(board, runtime, actorId))
			yield return option;
	}

	public static IReadOnlyList<Option> GetMoveOptions(
		BattleBoard board,
		ActorSession runtime,
		string actorId) =>
		MovePathFinder.Find(board, runtime, actorId);

	public static HashSet<Coord> GetMissileCells(
		BattleBoard board,
		ActorSession runtime,
		string actorId,
		EMissileMount mount,
		int range)
	{
		var def = MissileDef.For(mount, range);
		return def.Discover(board, runtime, actorId)
			.OfType<MissileAction>()
			.Select(missile => missile.Center)
			.ToHashSet();
	}

	public static bool IsRailgunAvailable(
		BattleBoard world,
		ActorSession runtime,
		string ownerId,
		string targetUnitId)
	{
		var action = new RailgunAction(ownerId, targetUnitId);
		return RailgunDef.Instance.IsLegal(action, world, runtime);
	}

	public static bool IsFlakAvailable(BattleBoard world, ActorSession runtime, string ownerId) =>
		Enum.GetValues<EFlakMount>().Any(mount =>
			FlakDef.For(mount).IsLegal(new FlakAction(ownerId, mount), world, runtime));

	public static HashSet<Coord> GetFlakBurstCells(
		BattleBoard board,
		ActorSession runtime,
		string ownerId,
		EFlakMount mount)
	{
		var action = new FlakAction(ownerId, mount);
		if (!FlakDef.For(mount).IsLegal(action, board, runtime))
			return [];

		var frame = BodyFrame.From(board.StateOf(ownerId));
		var config = FlakMountConfig.For(mount);
		return FlakTargeting.GetBurstCells(frame, config, board.Grid.IsInBounds);
	}
}
