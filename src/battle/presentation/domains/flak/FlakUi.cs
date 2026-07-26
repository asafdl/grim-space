using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Spatial;
using GrimSpace.Battle.Weapons;
using GrimSpace.Math.Grid;

namespace GrimSpace.Battle.Presentation.Domains.Flak;

public static class FlakUi
{
	public static FlakAction? Translate(string actorId, EFlakMount mount) =>
		new(actorId, mount);

	public static bool TryApply(
		BattleOrchestrator battle,
		Interaction.InteractionState state,
		Coord cell)
	{
		var actor = battle.GetActiveActor();
		if (actor is null || !battle.CanAct(actor))
			return false;

		var frame = BodyFrame.From(battle.Sim.StateOf<ActorState>(battle.PlayerId));
		var mount = FlakTargeting.MountForCell(frame, cell);
		if (mount is null)
			return false;

		var action = Translate(battle.PlayerId, mount.Value);
		if (action is null || !battle.Sim.TryEnqueue(action))
			return false;

		state.FlakHover = null;
		state.SetMoveMode();
		return true;
	}

	public static HashSet<Coord> GetBurstCells(BattleOrchestrator battle, EFlakMount mount)
	{
		if (battle.GetActiveActor() is null)
			return [];

		var sim = battle.Sim;
		var actorId = battle.PlayerId;
		var action = new FlakAction(actorId, mount);
		if (!FlakDef.For(mount).IsLegal(action, sim.World, sim.RuntimeFor(actorId)))
			return [];

		var frame = BodyFrame.From(sim.StateOf<ActorState>(actorId));
		var config = FlakMountConfig.For(mount);
		return FlakTargeting.GetBurstCells(frame, config, battle.Grid.IsInBounds);
	}

	public static HashSet<Coord> GetPreviewCells(BattleOrchestrator battle, Interaction.InteractionState state)
	{
		if (state.FlakHover is not Coord hover)
			return [];

		var frame = BodyFrame.From(battle.Sim.StateOf<ActorState>(battle.PlayerId));
		var mount = FlakTargeting.MountForCell(frame, hover);
		if (mount is null)
			return [];

		var action = Translate(battle.PlayerId, mount.Value);
		if (action is null || battle.Sim.Peek(action) is null)
			return [];

		return GetBurstCells(battle, mount.Value);
	}
}
