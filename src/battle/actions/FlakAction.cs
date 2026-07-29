using GrimSpace.Battle.World;
using GrimSpace.Battle.Effects;
using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.Spatial;
using GrimSpace.Battle.Weapons;
using GrimSpace.Core.Actions;

namespace GrimSpace.Battle.Actions;

public sealed record FlakAction(
	string ActorId,
	EFlakMount Mount) : IAction<BattleWorld, ActorRuntime>
{
	public IActionDef<IAction, BattleWorld, ActorRuntime, IEffect<BattleWorld, ActorRuntime>> Definition =>
		FlakDef.For(Mount);
}

public sealed class FlakDef(EFlakMount mount)
	: IActionDef<IAction, BattleWorld, ActorRuntime, IEffect<BattleWorld, ActorRuntime>>
{
	public EFlakMount Mount { get; } = mount;

	public static FlakDef For(EFlakMount mount) => new(mount);

	public IEnumerable<IAction> Discover(BattleWorld world, ActorRuntime runtime, string actorId)
	{
		var action = Bind(actorId);
		if (IsPossible(action, world, runtime))
			yield return action;
	}

	public FlakAction Bind(string actorId) => new(actorId, Mount);

	public bool IsPossible(IAction action, BattleWorld world, ActorRuntime runtime) =>
		IsPossible(Cast(action), world, runtime);

	public bool IsLegal(IAction action, BattleWorld world, ActorRuntime runtime) =>
		IsLegal(Cast(action), world, runtime);

	public IReadOnlyList<IEffect<BattleWorld, ActorRuntime>> Resolve(
		IAction action,
		BattleWorld world,
		ActorRuntime runtime) =>
		Resolve(Cast(action), world, runtime);

	public bool IsPossible(FlakAction action, BattleWorld world, ActorRuntime runtime)
	{
		var frame = BodyFrame.From(world.StateOf(action.ActorId));
		var config = FlakMountConfig.For(action.Mount);
		return FlakTargeting.IsValidBurst(frame, config, world.Grid.IsInBounds);
	}

	public bool IsLegal(FlakAction action, BattleWorld world, ActorRuntime runtime)
	{
		if (world.StateOf(action.ActorId).FlakRemaining <= 0)
			return false;

		return IsPossible(action, world, runtime);
	}

	public IReadOnlyList<IEffect<BattleWorld, ActorRuntime>> Resolve(
		FlakAction action,
		BattleWorld world,
		ActorRuntime runtime)
	{
		var frame = BodyFrame.From(world.StateOf(action.ActorId));
		var config = FlakMountConfig.For(action.Mount);
		var cells = FlakTargeting.GetBurstCells(frame, config, world.Grid.IsInBounds);

		return
		[
			new ScheduleActionEffect(
				CombatConfig.FlakResolveDelay,
				ResolveHazardDef.Instance.Bind(
					action.ActorId,
					EHazardKind.FlakBurst,
					cells,
					damage: CombatConfig.FlakDamage,
					CombatConfig.FlakMomentumLoss)),
			new FlakChangeEffect(-1),
		];
	}

	private static FlakAction Cast(IAction action) =>
		action as FlakAction ?? throw new ArgumentException($"Expected {nameof(FlakAction)}.", nameof(action));
}
