using GrimSpace.Battle.Effects;
using GrimSpace.Battle.Slices;
using GrimSpace.Battle.Spatial;
using GrimSpace.Battle.Weapons;
using GrimSpace.Core.Actions;

namespace GrimSpace.Battle.Actions;

public sealed class FlakDef(EFlakMount mount) : IActionDef
{
	public EFlakMount Mount { get; } = mount;

	public static FlakDef For(EFlakMount mount) => new(mount);

	public IEnumerable<IAction> Discover(BattleActionContext ctx, string ownerId)
	{
		var action = new FlakAction(ownerId, Mount);
		if (IsPossible(action, ctx))
			yield return action;
	}

	public bool IsPossible(IAction action, BattleActionContext ctx) =>
		IsPossible(Cast(action), ctx);

	public bool IsLegal(IAction action, BattleActionContext ctx) =>
		IsLegal(Cast(action), ctx);

	public IReadOnlyList<IEffect<BattleSlices>> Resolve(IAction action, BattleActionContext ctx) =>
		Resolve(Cast(action), ctx);

	public bool IsPossible(FlakAction action, BattleActionContext ctx)
	{
		var frame = BodyFrame.From(ctx.Board.StateOf(action.OwnerId));
		var config = FlakMountConfig.For(action.Mount);
		return FlakTargeting.IsValidBurst(frame, config, ctx.Board.Grid.IsInBounds);
	}

	public bool IsLegal(FlakAction action, BattleActionContext ctx)
	{
		if (ctx.PhaseContext.FlakUsedThisTurn)
			return false;

		return IsPossible(action, ctx);
	}

	public IReadOnlyList<IEffect<BattleSlices>> Resolve(FlakAction action, BattleActionContext ctx)
	{
		var board = ctx.Board;
		var frame = BodyFrame.From(board.StateOf(action.OwnerId));
		var config = FlakMountConfig.For(action.Mount);
		var cells = FlakTargeting.GetBurstCells(frame, config, board.Grid.IsInBounds);
		var hazardId = board.IdRegistry.NextNonUnitId("flak-burst");

		return
		[
			new SpawnFlakHazardEffect(hazardId, cells),
			new ScheduleActionEffect(
				CombatConfig.FlakResolveDelay,
				new ResolveHazardAction(action.OwnerId, hazardId)),
			new MarkFlakUsedEffect(),
		];
	}

	private static FlakAction Cast(IAction action) =>
		action as FlakAction ?? throw new ArgumentException($"Expected {nameof(FlakAction)}.", nameof(action));
}
