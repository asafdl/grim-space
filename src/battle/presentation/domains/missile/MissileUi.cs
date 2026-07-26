using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Board;
using GrimSpace.Battle.Spatial;
using GrimSpace.Battle.Weapons;
using GrimSpace.Core;
using GrimSpace.Math.Grid;
using BoundedGrid = GrimSpace.Math.Grid.Grid;

namespace GrimSpace.Battle.Presentation.Domains.Missile;

public static class MissileUi
{
	public static MissileAction? Translate(
		string actorId,
		Coord center,
		EMissileMount mount,
		int range) =>
		new(actorId, center, mount, range);

	public static bool TryApply(
		BattleOrchestrator battle,
		Interaction.InteractionState state,
		Coord center)
	{
		if (state.MissileMount is not EMissileMount mount)
			return false;

		var actor = battle.GetActiveActor();
		if (actor is null || !battle.CanAct(actor))
			return false;

		var action = Translate(battle.PlayerId, center, mount, state.MissileRange);
		if (action is null || !battle.Sim.TryEnqueue(action))
			return false;

		state.MissileHover = null;
		return true;
	}

	public static HashSet<Coord> GetValidTargetCells(
		BattleOrchestrator battle,
		EMissileMount mount,
		int range)
	{
		var sim = battle.Sim;
		var actorId = battle.PlayerId;
		return MissileDef.For(mount, range)
			.Discover(sim.World, sim.RuntimeFor(actorId), actorId)
			.OfType<MissileAction>()
			.Select(missile => missile.Center)
			.ToHashSet();
	}

	public static HashSet<Coord> GetPreviewCells(
		BattleOrchestrator battle,
		Interaction.InteractionState state)
	{
		if (state.MissileMount is not EMissileMount mount
			|| state.MissileHover is not Coord hover)
		{
			return [];
		}

		var action = Translate(battle.PlayerId, hover, mount, state.MissileRange);
		if (action is null || battle.Sim.Peek(action) is null)
			return [];

		return GetBlastCells(hover, battle.Grid);
	}

	public static bool IsHoverLegal(BattleOrchestrator battle, Interaction.InteractionState state) =>
		state.MissileHover is Coord hover
		&& state.MissileMount is EMissileMount mount
		&& battle.Sim.Peek(Translate(battle.PlayerId, hover, mount, state.MissileRange)!) is not null;

	public static HashSet<Coord> GetBlastCells(Coord center, BoundedGrid grid) =>
		Hazard.MissileZone(
			"preview",
			EntityIds.World,
			center,
			BodyFrame.WorldAligned(center),
			grid,
			CombatConfig.MissileRadius,
			CombatConfig.MissileDamage,
			CombatConfig.MissileMomentumLoss).Cells;
}
