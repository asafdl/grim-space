using GrimSpace.Battle.Board;
using GrimSpace.Battle.Effects;
using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.Spatial;
using GrimSpace.Battle.Weapons;
using GrimSpace.Core.Actions;

namespace GrimSpace.Battle.Actions;

public sealed class FlakDef(EFlakMount mount)
	: IActionDef<IAction, BattleBoard, ActorSession, IEffect<BattleBoard, ActorSession>>
{
	public EFlakMount Mount { get; } = mount;

	public static FlakDef For(EFlakMount mount) => new(mount);

	public IEnumerable<IAction> Discover(BattleBoard world, ActorSession runtime, string ownerId)
	{
		var action = new FlakAction(ownerId, Mount);
		if (IsPossible(action, world, runtime))
			yield return action;
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

	public bool IsPossible(FlakAction action, BattleBoard world, ActorSession runtime)
	{
		var frame = BodyFrame.From(world.StateOf(action.OwnerId));
		var config = FlakMountConfig.For(action.Mount);
		return FlakTargeting.IsValidBurst(frame, config, world.Grid.IsInBounds);
	}

	public bool IsLegal(FlakAction action, BattleBoard world, ActorSession runtime)
	{
		if (runtime.FlakUsedThisTurn)
			return false;

		return IsPossible(action, world, runtime);
	}

	public IReadOnlyList<IEffect<BattleBoard, ActorSession>> Resolve(
		FlakAction action,
		BattleBoard world,
		ActorSession runtime)
	{
		var frame = BodyFrame.From(world.StateOf(action.OwnerId));
		var config = FlakMountConfig.For(action.Mount);
		var cells = FlakTargeting.GetBurstCells(frame, config, world.Grid.IsInBounds);
		var hazardId = world.IdRegistry.NextNonUnitId("flak-burst");

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
