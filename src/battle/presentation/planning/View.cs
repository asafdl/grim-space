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
		var actorId = battle.OwnerId;
		var session = battle.Session;
		if (session.PreviewRuntime.IsMovePathStarted)
			return [];

		return ActionQueries.GetMoveOptions(session.PreviewWorld, session.PreviewRuntime, actorId);
	}

	public static HashSet<Coord> GetMissileTargetHighlights(
		BattleOrchestrator battle,
		EMissileMount mount,
		int range) =>
		ActionQueries.GetMissileCells(
			battle.Board,
			battle.Runtime,
			battle.OwnerId,
			mount,
			range);

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
		if (!ActionQueries.IsRailgunAvailable(
			battle.Board,
			battle.Runtime,
			battle.OwnerId,
			enemy.State.Id))
			return cells;

		cells.Add(battle.Board.StateOf(enemy.State.Id).Position);
		return cells;
	}

	public static HashSet<Coord> GetFlakBurstHighlights(BattleOrchestrator battle, EFlakMount mount) =>
		ActionQueries.GetFlakBurstCells(battle.Board, battle.Runtime, battle.OwnerId, mount);
}
