using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Presentation.Ui;
using GrimSpace.Battle.Spatial;
using GrimSpace.Battle.World;
using GrimSpace.Math.Grid;

namespace GrimSpace.Battle.Presentation.Domains.Railgun;

public static class RailgunUi
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
		var burstCells = WeaponBursts.RailgunBurstCells(frame, battle.Layout.Grid.IsInBounds);
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
		if (battle.GetActiveUnit() is not { State.Id: var id } || id != battle.PlayerId)
			return [];

		var sim = battle.Sim;
		var actorId = battle.PlayerId;
		var action = new RailgunAction(actorId);
		if (!RailgunDef.Instance.IsLegal(action, sim.World, sim.RuntimeFor(actorId)))
			return [];

		return GetBurstCellsGeometry(battle);
	}

	public static HashSet<Coord> GetBurstCellsGeometry(BattleOrchestrator battle)
	{
		var frame = BodyFrame.From(battle.Sim.StateOf<ActorState>(battle.PlayerId));
		return WeaponBursts.RailgunBurstCells(frame, battle.Layout.Grid.IsInBounds);
	}

	public static HashSet<string> GetThreatenedUnitIds(BattleOrchestrator battle) =>
		WeaponThreatPreview.UnitIdsInCells(battle, GetBurstCells(battle));

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
