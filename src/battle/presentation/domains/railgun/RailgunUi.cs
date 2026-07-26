using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Units;
using GrimSpace.Math.Grid;

namespace GrimSpace.Battle.Presentation.Domains.Railgun;

public static class RailgunUi
{
	public static RailgunAction? Translate(string actorId, string targetId) =>
		new(actorId, targetId);

	public static bool TryApply(BattleOrchestrator battle, Interaction.InteractionState state, Unit target)
	{
		var actor = battle.GetActiveActor();
		if (actor is null || !battle.CanAct(actor))
			return false;

		var action = Translate(battle.PlayerId, target.State.Id);
		if (action is null || !battle.Sim.TryEnqueue(action))
			return false;

		state.RailgunHover = null;
		return true;
	}

	public static bool IsTargetLegal(BattleOrchestrator battle, Unit target)
	{
		var enemy = battle.GetEnemy();
		if (enemy is null || target.State.Id != enemy.State.Id)
			return false;

		var action = Translate(battle.PlayerId, target.State.Id);
		return action is not null && battle.Sim.Peek(action) is not null;
	}

	public static HashSet<Coord> GetTargetCells(BattleOrchestrator battle, Unit? actor)
	{
		var cells = new HashSet<Coord>();
		if (actor is null || !battle.CanAct(actor))
			return cells;

		var enemy = battle.Opponent;
		if (!IsTargetLegal(battle, enemy))
			return cells;

		cells.Add(battle.Sim.StateOf<ActorState>(enemy.State.Id).Position);
		return cells;
	}

	public static Coord? GetHoveredCell(BattleOrchestrator battle, Interaction.InteractionState state)
	{
		if (state.RailgunHover is not Unit target || !IsTargetLegal(battle, target))
			return null;

		var peek = battle.Sim.Peek(Translate(battle.PlayerId, target.State.Id)!);
		return peek?.World.StateOf(battle.Opponent.State.Id).Position;
	}
}
