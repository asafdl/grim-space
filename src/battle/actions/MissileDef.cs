using GrimSpace.Battle.Board;
using GrimSpace.Battle.Effects;
using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.Spatial;
using GrimSpace.Battle.Weapons;
using GrimSpace.Core.Actions;

namespace GrimSpace.Battle.Actions;

public sealed class MissileDef(EMissileMount mount, int range)
	: IActionDef<IAction, BattleBoard, ActorSession, IEffect<BattleBoard, ActorSession>>
{
	public EMissileMount Mount { get; } = mount;
	public int Range { get; } = range;

	public static MissileDef For(EMissileMount mount, int range) => new(mount, range);

	public IEnumerable<IAction> Discover(BattleBoard world, ActorSession runtime, string ownerId)
	{
		var frame = BodyFrame.From(world.StateOf(ownerId));
		var config = MissileMountConfig.For(Mount).WithRange(Range);
		foreach (var cell in MissileTargeting.GetValidCells(frame, config, world.Grid.IsInBounds))
		{
			var action = new MissileAction(ownerId, cell, Mount, Range);
			if (IsPossible(action, world, runtime))
				yield return action;
		}
	}

	public bool IsPossible(IAction action, BattleBoard world, ActorSession runtime) =>
		IsPossible(Cast(action), world, runtime);

	public bool IsLegal(IAction action, BattleBoard world, ActorSession runtime) =>
		IsLegal(Cast(action), world, runtime);

	public IReadOnlyList<IEffect<BattleBoard, ActorSession>> Resolve(
		IAction action,
		BattleBoard world,
		ActorSession runtime) =>
		Resolve(Cast(action), world, runtime);

	public bool IsPossible(MissileAction action, BattleBoard world, ActorSession runtime)
	{
		if (world.StateOf(action.OwnerId).MissilesRemaining <= 0)
			return false;

		var frame = BodyFrame.From(world.StateOf(action.OwnerId));
		var config = MissileMountConfig.For(action.Mount).WithRange(action.Range);
		return MissileTargeting.IsValidTarget(frame, action.Center, config, world.Grid.IsInBounds);
	}

	public bool IsLegal(MissileAction action, BattleBoard world, ActorSession runtime) =>
		IsPossible(action, world, runtime);

	public IReadOnlyList<IEffect<BattleBoard, ActorSession>> Resolve(
		MissileAction action,
		BattleBoard world,
		ActorSession runtime)
	{
		var hazardId = world.IdRegistry.NextNonUnitId("missile-zone");
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
