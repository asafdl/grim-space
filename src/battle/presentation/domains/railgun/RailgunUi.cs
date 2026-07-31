using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Presentation.Ui;
using GrimSpace.Battle.Spatial;
using GrimSpace.Battle.Weapons;
using GrimSpace.Math.Grid;

namespace GrimSpace.Battle.Presentation.Domains.Railgun;

public static class RailgunUi
{
	public static bool TryApply(
		BattleOrchestrator battle,
		Interaction.InteractionState state,
		Coord cell)
	{
		var actor = battle.GetActiveActor();
		if (actor is null || !battle.CanAct(actor))
			return false;

		var frame = BodyFrame.From(battle.Sim.StateOf<ActorState>(battle.PlayerId));
		var burstCells = RailgunTargeting.GetBurstCells(frame, battle.Layout.Grid.IsInBounds);
		if (!burstCells.Contains(cell))
			return false;

		var action = new RailgunAction(battle.PlayerId);
		if (!battle.Sim.TryEnqueue(action))
			return false;

		state.SetMode(EPlayerMode.Move);
		return true;
	}

	public static HashSet<Coord> GetBurstCells(BattleOrchestrator battle)
	{
		if (battle.GetActiveActor() is null)
			return [];

		var sim = battle.Sim;
		var actorId = battle.PlayerId;
		var action = new RailgunAction(actorId);
		if (!RailgunDef.Instance.IsLegal(action, sim.World, sim.RuntimeFor(actorId)))
			return [];

		var frame = BodyFrame.From(sim.StateOf<ActorState>(actorId));
		return RailgunTargeting.GetBurstCells(frame, battle.Layout.Grid.IsInBounds);
	}

	public static HashSet<Coord> GetPreviewCells(BattleOrchestrator battle, Interaction.InteractionState state)
	{
		if (state.RailgunHover is not Coord hover)
			return [];

		var burstCells = GetBurstCells(battle);
		if (!burstCells.Contains(hover))
			return [];

		var action = new RailgunAction(battle.PlayerId);
		if (battle.Sim.Peek(action) is null)
			return [];

		return burstCells;
	}
}
