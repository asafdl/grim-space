using GrimSpace.Battle.Actions;
using GrimSpace.Battle.World;
using GrimSpace.Core.Actions;
using GrimSpace.Battle.Presentation.Ui;
using GrimSpace.Battle.Units;
using GrimSpace.Math.Grid;

namespace GrimSpace.Battle.Presentation.Domains.Turn;

public static class TurnUi
{
	public static bool TryCommit(BattleOrchestrator battle, out IReadOnlyList<IAction> playerActions)
	{
		playerActions = [];

		if (battle.IsBattleOver)
			return false;

		if (!battle.Sim.TryCommit(out var actions, out _))
			return false;

		playerActions = HeadingDef.Instance.Streamline(actions, battle.Sim.UndoGroups).ToList();
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

	public static BattleWorld GetPreviewWorld(BattleOrchestrator battle)
	{
		var peek = battle.Sim.Peek(EndOfPhaseDef.Instance.Bind(battle.PlayerId));
		return peek?.World ?? battle.Sim.World;
	}

	public static HashSet<Coord> GetPreviewHazardCells(BattleOrchestrator battle)
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
		int queuedActionCount,
		bool missileInRange,
		int moveRingCount,
		int activeRingIndex,
		string? activeRingHint)
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
			queuedActionCount,
			state.RailgunHover,
			state.MissileMount,
			state.MissileRange,
			state.MissileHover,
			missileInRange,
			moveRingCount,
			activeRingIndex,
			activeRingHint);
	}
}
