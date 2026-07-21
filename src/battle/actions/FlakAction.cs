using GrimSpace.Battle.Weapons;
using GrimSpace.Core.Actions.Battle;
using GrimSpace.Battle.Spatial;
using GrimSpace.Core.Actions;
using GrimSpace.Battle.Slices;
using GrimSpace.Battle.Effects;

namespace GrimSpace.Battle.Actions;

public sealed class FlakAction(string ownerId, EFlakMount mount, int? undoGroup = null) : IBattleAction
{
	public string OwnerId { get; } = ownerId;
	public int? UndoGroup { get; } = undoGroup;
	public EFlakMount Mount { get; } = mount;

	public bool IsLegal(BattleBoard board, TurnState state, IEnumerable<IAction> applied)
	{
		if (applied.Any(action => action is FlakAction))
			return false;

		var frame = BodyFrame.From(board.StateOf(OwnerId));
		var config = FlakMountConfig.For(Mount);
		return FlakTargeting.IsValidBurst(frame, config, board.Grid.IsInBounds);
	}

	public IReadOnlyList<IEffect<BattleSlices>> Resolve(BattleBoard board, TurnState state, IEnumerable<IAction> applied)
	{
		var frame = BodyFrame.From(board.StateOf(OwnerId));
		var config = FlakMountConfig.For(Mount);
		var cells = FlakTargeting.GetBurstCells(frame, config, board.Grid.IsInBounds);
		var hazardId = board.IdRegistry.NextNonUnitId("flak-burst");

		return
		[
			new SpawnFlakHazardEffect(hazardId, cells),
			new ScheduleActionEffect(
				CombatConfig.FlakResolveDelay,
				new ResolveHazardAction(OwnerId, hazardId)),
		];
	}
}
