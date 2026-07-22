using GrimSpace.Battle.Board;
using GrimSpace.Battle.Effects;
using GrimSpace.Battle.Slices;
using GrimSpace.Battle.Spatial;
using GrimSpace.Battle.Weapons;
using GrimSpace.Core.Actions;

namespace GrimSpace.Battle.Actions;

public sealed class MissileDef(EMissileMount mount, int range) : IActionDef
{
	public EMissileMount Mount { get; } = mount;
	public int Range { get; } = range;

	public static MissileDef For(EMissileMount mount, int range) => new(mount, range);

	public IEnumerable<IAction> Discover(BattleActionContext ctx, string ownerId)
	{
		var frame = BodyFrame.From(ctx.Board.StateOf(ownerId));
		var config = MissileMountConfig.For(Mount).WithRange(Range);
		foreach (var cell in MissileTargeting.GetValidCells(frame, config, ctx.Board.Grid.IsInBounds))
		{
			var action = new MissileAction(ownerId, cell, Mount, Range);
			if (IsPossible(action, ctx))
				yield return action;
		}
	}

	public bool IsPossible(IAction action, BattleActionContext ctx) =>
		IsPossible(Cast(action), ctx);

	public bool IsLegal(IAction action, BattleActionContext ctx) =>
		IsLegal(Cast(action), ctx);

	public IReadOnlyList<IEffect<BattleSlices>> Resolve(IAction action, BattleActionContext ctx) =>
		Resolve(Cast(action), ctx);

	public bool IsPossible(MissileAction action, BattleActionContext ctx)
	{
		if (ctx.Board.StateOf(action.OwnerId).MissilesRemaining <= 0)
			return false;

		var frame = BodyFrame.From(ctx.Board.StateOf(action.OwnerId));
		var config = MissileMountConfig.For(action.Mount).WithRange(action.Range);
		return MissileTargeting.IsValidTarget(frame, action.Center, config, ctx.Board.Grid.IsInBounds);
	}

	public bool IsLegal(MissileAction action, BattleActionContext ctx) => IsPossible(action, ctx);

	public IReadOnlyList<IEffect<BattleSlices>> Resolve(MissileAction action, BattleActionContext ctx)
	{
		var board = ctx.Board;
		var hazardId = board.IdRegistry.NextNonUnitId("missile-zone");
		return
		[
			new SpawnHazardEffect(hazardId, action.Center, EHazardKind.MissileZone),
			new ScheduleActionEffect(
				CombatConfig.MissileResolveDelay,
				new ResolveHazardAction(action.OwnerId, hazardId)),
			new MissileChangeEffect(-1),
		];
	}

	private static MissileAction Cast(IAction action) =>
		action as MissileAction ?? throw new ArgumentException($"Expected {nameof(MissileAction)}.", nameof(action));
}
