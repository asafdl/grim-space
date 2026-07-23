using GrimSpace.Battle.Board;
using GrimSpace.Battle.Movement;
using GrimSpace.Battle.Spatial;
using GrimSpace.Battle.Units;
using GrimSpace.Battle.Weapons;
using GrimSpace.Core;
using GrimSpace.Battle.Actions;
using GrimSpace.Math.Grid;
using BoundedGrid = GrimSpace.Math.Grid.Grid;

namespace GrimSpace.Battle.Presentation.Planning;

/// <summary>
/// Presentation queries: projects rules state into display data (highlights, ghosts, overlays).
/// </summary>
public static class View
{
	public static IReadOnlyList<Option> GetMoveHighlights(BattleOrchestrator battle, Unit? actor)
	{
		if (actor is null || !battle.CanAct(actor))
			return [];

		return GetLegalMoves(battle);
	}

	public static BattleBoard GetTurnGhost(BattleOrchestrator battle) => battle.Board;

	public static HashSet<Coord> GetPlannedHazardHighlights(BattleOrchestrator battle)
	{
		var cells = new HashSet<Coord>();
		foreach (var hazard in battle.Board.TurnHazards)
			cells.UnionWith(hazard.Cells);

		return cells;
	}

	public static IReadOnlyList<Option> GetLegalMoves(BattleOrchestrator battle)
	{
		var actorId = battle.PlayerId;
		var session = battle.Session;
		if (session.PreviewActorRuntimes.For(actorId).IsMovePathStarted)
			return [];

		var board = session.PreviewWorld;
		var runtime = session.PreviewActorRuntimes.For(actorId);
		var unitType = board.StateOf(actorId).Type;

		return Capabilities.For(unitType)
			.OfType<MoveDef>()
			.SelectMany(def => def.DiscoverPaths(board, runtime, actorId))
			.ToList();
	}

	public static HashSet<Coord> GetMissileTargetHighlights(
		BattleOrchestrator battle,
		EMissileMount mount,
		int range)
	{
		var board = battle.Board;
		var actorId = battle.PlayerId;
		return MissileDef.For(mount, range)
			.Discover(board, battle.Runtime, actorId)
			.OfType<MissileAction>()
			.Select(missile => missile.Center)
			.ToHashSet();
	}

	public static HashSet<Coord> GetMissileBlastHighlights(Coord center, BoundedGrid grid) =>
		Hazard.MissileZone(
			"preview",
			EntityIds.World,
			center,
			BodyFrame.WorldAligned(center),
			grid,
			CombatConfig.MissileRadius,
			CombatConfig.MissileDamage,
			CombatConfig.MissileMomentumLoss).Cells;

	public static HashSet<Coord> GetRailgunTargetHighlights(BattleOrchestrator battle, Unit? actor)
	{
		var cells = new HashSet<Coord>();
		if (actor is null || !battle.CanAct(actor))
			return cells;

		var enemy = battle.Opponent;
		var action = new RailgunAction(battle.PlayerId, enemy.State.Id);
		if (!RailgunDef.Instance.IsLegal(action, battle.Board, battle.Runtime))
			return cells;

		cells.Add(battle.Board.StateOf(enemy.State.Id).Position);
		return cells;
	}

	public static HashSet<Coord> GetFlakBurstHighlights(BattleOrchestrator battle, EFlakMount mount)
	{
		var board = battle.Board;
		var actorId = battle.PlayerId;
		var action = new FlakAction(actorId, mount);
		if (!FlakDef.For(mount).IsLegal(action, board, battle.Runtime))
			return [];

		var frame = BodyFrame.From(board.StateOf(actorId));
		var config = FlakMountConfig.For(mount);
		return FlakTargeting.GetBurstCells(frame, config, board.Grid.IsInBounds);
	}
}
