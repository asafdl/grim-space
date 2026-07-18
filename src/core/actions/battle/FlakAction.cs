using GrimSpace.Battle.Weapons;
using GrimSpace.Battle.Spatial;
using GrimSpace.Core.Actions;
using GrimSpace.Core.Actions.Battle.Contexts;
using GrimSpace.Core.Actions.Battle.Effects;

namespace GrimSpace.Core.Actions.Battle;

public sealed class FlakAction(string ownerId, EFlakMount mount) : IAction
{
	public string OwnerId { get; } = ownerId;
	public EFlakMount Mount { get; } = mount;

	public bool IsLegal(BattleBoard board, BattlePlanContext context)
	{
		if (context.PhaseActions.Any(action => action is FlakAction))
			return false;

		var frame = BodyFrame.From(board.StateOf(OwnerId));
		var config = FlakMountConfig.For(Mount);
		return FlakTargeting.IsValidBurst(frame, config, board.Grid.IsInBounds);
	}

	public IReadOnlyList<IEffect<BattleSlices>> Resolve(BattleBoard board, BattlePlanContext context)
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
