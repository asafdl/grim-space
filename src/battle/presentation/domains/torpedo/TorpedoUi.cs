using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Presentation.Ui;
using GrimSpace.Battle.Weapons;
using GrimSpace.Battle.World;
using GrimSpace.Math.Grid;

namespace GrimSpace.Battle.Presentation.Domains.Torpedo;

public static class TorpedoUi
{
	public static bool TryApply(
		BattleOrchestrator battle,
		Interaction.InteractionState state,
		Coord cell)
	{
		var actor = battle.GetActiveUnit();
		if (actor is null || actor.State.Id != battle.PlayerId || !battle.CanAct(actor))
			return false;

		if (MountForCell(battle, cell) is not { } mount)
			return false;

		var action = new TorpedoAction(battle.PlayerId, mount);
		if (!battle.Sim.TryEnqueue(action))
			return false;

		state.SetMode(EPlayerMode.Move);
		return true;
	}

	public static HashSet<Coord> GetMountCells(BattleOrchestrator battle)
	{
		if (battle.GetActiveUnit() is not { State.Id: var id } || id != battle.PlayerId)
			return [];

		var sim = battle.Sim;
		var actorId = battle.PlayerId;
		var ship = sim.StateOf<ActorState>(actorId);
		var cells = new HashSet<Coord>();

		foreach (var mount in TorpedoConfig.EnabledMounts)
		{
			var action = new TorpedoAction(actorId, mount);
			if (!TorpedoDef.For(mount).IsLegal(action, sim.World, sim.RuntimeFor(actorId)))
				continue;

			var (position, _, _) = TorpedoMount.LaunchPose(ship, mount);
			cells.Add(position);
		}

		return cells;
	}

	public static HashSet<Coord> GetPreviewCells(
		BattleOrchestrator battle,
		Interaction.InteractionState state)
	{
		if (state.TorpedoHover is not Coord hover)
			return [];

		if (MountForCell(battle, hover) is null)
			return [];

		return [hover];
	}

	public static ETorpedoMount? MountForCell(BattleOrchestrator battle, Coord cell)
	{
		var ship = battle.Sim.StateOf<ActorState>(battle.PlayerId);
		foreach (var mount in TorpedoConfig.EnabledMounts)
		{
			var (position, _, _) = TorpedoMount.LaunchPose(ship, mount);
			if (position == cell)
				return mount;
		}

		return null;
	}
}
