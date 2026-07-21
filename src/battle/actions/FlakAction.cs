using GrimSpace.Battle.Weapons;
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

	public bool IsLegal(BattleActionContext ctx)
	{
		if (ctx.TurnState.FlakUsedThisTurn)
			return false;

		var frame = BodyFrame.From(ctx.Board.StateOf(OwnerId));
		var config = FlakMountConfig.For(Mount);
		return FlakTargeting.IsValidBurst(frame, config, ctx.Board.Grid.IsInBounds);
	}

	public IReadOnlyList<IEffect<BattleSlices>> Resolve(BattleActionContext ctx)
	{
		var board = ctx.Board;
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
			new MarkFlakUsedEffect(),
		];
	}
}
