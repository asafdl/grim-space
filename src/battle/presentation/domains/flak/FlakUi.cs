using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Presentation.Ui;
using GrimSpace.Battle.Spatial;
using GrimSpace.Battle.Weapons;
using GrimSpace.Battle.World;
using GrimSpace.Math.Grid;

namespace GrimSpace.Battle.Presentation.Domains.Flak;

public static class FlakUi
{
	public static bool TryApply(
		BattleOrchestrator battle,
		Interaction.InteractionState state,
		Coord cell)
	{
		var actor = battle.GetActiveUnit();
		if (actor is null || actor.State.Id != battle.PlayerId || !battle.CanAct(actor))
			return false;

		var frame = BodyFrame.From(battle.Sim.StateOf<ActorState>(battle.PlayerId));
		var mount = WeaponBursts.FlakMountForCell(frame, cell);
		if (mount is null)
			return false;

		var action = new FlakAction(battle.PlayerId, mount.Value);
		if (!battle.Sim.TryEnqueue(action))
			return false;

		state.SetMode(EPlayerMode.Move);
		return true;
	}

	public static HashSet<Coord> GetBurstCells(BattleOrchestrator battle, EFlakMount mount)
	{
		if (battle.GetActiveUnit() is not { State.Id: var id } || id != battle.PlayerId)
			return [];

		var sim = battle.Sim;
		var actorId = battle.PlayerId;
		var action = new FlakAction(actorId, mount);
		if (!FlakDef.For(mount).IsLegal(action, sim.World, sim.RuntimeFor(actorId)))
			return [];

		var frame = BodyFrame.From(sim.StateOf<ActorState>(actorId));
		var config = FlakMountConfig.For(mount);
		return WeaponBursts.FlakBurstCells(frame, config, battle.Layout.Grid.IsInBounds);
	}

	public static HashSet<Coord> GetPreviewCells(BattleOrchestrator battle, Interaction.InteractionState state)
	{
		if (state.FlakHover is not Coord hover)
			return [];

		var frame = BodyFrame.From(battle.Sim.StateOf<ActorState>(battle.PlayerId));
		var mount = WeaponBursts.FlakMountForCell(frame, hover);
		if (mount is null)
			return [];

		var action = new FlakAction(battle.PlayerId, mount.Value);
		if (battle.Sim.Peek(action) is null)
			return [];

		return GetBurstCells(battle, mount.Value);
	}
}
