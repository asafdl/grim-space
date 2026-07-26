using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Board;
using GrimSpace.Core.Actions;
using GrimSpace.Battle.Presentation.Events;
using GrimSpace.Battle.Presentation.Ui;
using GrimSpace.Battle.Units;
using GrimSpace.Math.Grid;

namespace GrimSpace.Battle.Presentation.Domains.Turn;

public static class TurnUi
{
	public static bool TryEndTurn(BattleOrchestrator battle, IPresentationEventSink? sink = null)
	{
		if (battle.IsBattleOver)
			return false;

		if (!battle.Sim.TryCommit(out var actions, out _))
			return false;

		actions = HeadingDef.Instance.Streamline(actions, battle.Sim.UndoGroups).ToList();
		battle.ResolveTurn(actions, sink);
		return true;
	}

	public static bool TryUndo(BattleOrchestrator battle, Interaction.InteractionState state)
	{
		if (!battle.Sim.TryUndoLast())
			return false;

		state.ClearInteraction();
		state.CommittedMovePath = [];
		return true;
	}

	public static BattleBoard GetTurnGhost(BattleOrchestrator battle)
	{
		var peek = battle.Sim.Peek(EndOfPhaseDef.Instance.Bind(battle.PlayerId));
		return peek?.World ?? battle.Sim.World;
	}

	public static HashSet<Coord> GetPlannedHazardCells(BattleOrchestrator battle)
	{
		var sim = battle.Sim;
		var cells = new HashSet<Coord>();
		for (var tick = sim.AnchorTick + 1; tick <= sim.TimelineMaxTick; tick++)
		{
			foreach (var action in sim.PeekTimeline(tick).OfType<ResolveHazardAction>())
				cells.UnionWith(action.Cells);
		}

		return cells;
	}

	public static string BuildHint(
		BattleOrchestrator battle,
		Interaction.InteractionState state,
		Unit? actor,
		Units.State actorState,
		int plannedActionCount,
		bool missileInRange)
	{
		if (actor is null)
			return "No active unit  |  WASD: pan  |  scroll/+/-: zoom  |  RMB: orbit  |  MMB: drag pan";

		var turnPrefix = battle.IsBattleOver
			? $"Battle over — winner: {battle.WinnerId}  |  "
			: $"Turn {battle.TurnNumber}  |  ";

		return turnPrefix + CombatHints.BuildHint(
			state.Mode,
			actorState,
			actorState.MissilesRemaining,
			plannedActionCount,
			state.RailgunHover,
			state.MissileMount,
			state.MissileRange,
			state.MissileHover,
			missileInRange);
	}
}
