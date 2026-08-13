using GrimSpace.Battle.World;
using GrimSpace.Battle.Effects;
using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.Spatial;
using GrimSpace.Battle.Abilities;
using GrimSpace.Core.Actions;
using GrimSpace.Math.Grid;

namespace GrimSpace.Battle.Actions;

public sealed record FlakAction(
	string ActorId,
	ESpatialOrientation MountedOn) : IAction<BattleWorld, ActorRuntime>
{
	public IActionDef<IAction, BattleWorld, ActorRuntime, IEffect<BattleWorld, ActorRuntime>> Definition =>
		FlakDef.Instance;
}

public sealed class FlakDef
	: IActionDef<IAction, BattleWorld, ActorRuntime, IEffect<BattleWorld, ActorRuntime>>
{
	public static FlakDef Instance { get; } = new();

	private static readonly ESpatialOrientation[] MountedOn =
	[
		ESpatialOrientation.Port,
		ESpatialOrientation.Starboard,
	];

	public IEnumerable<IAction> Discover(BattleWorld world, ActorRuntime runtime, string actorId)
	{
		foreach (var mountedOn in MountedOn)
		{
			var action = Bind(actorId, mountedOn);
			if (IsPossible(action, world, runtime))
				yield return action;
		}
	}

	public FlakAction Bind(string actorId, ESpatialOrientation mountedOn) =>
		new(actorId, mountedOn);

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
		return WeaponBursts.IsValidFlakBurst(frame, action.MountedOn, world.Grid.IsInBounds);
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
		var cells = WeaponBursts.FlakBurstCells(frame, action.MountedOn, world.Grid.IsInBounds);

		return
		[
			new ResolveHazardEffect(
				EHazardKind.FlakBurst,
				cells,
				CombatConfig.FlakDamage,
				CombatConfig.FlakMomentumLoss),
			new FlakChangeEffect(-1),
		];
	}

	private static FlakAction Cast(IAction action) =>
		action as FlakAction ?? throw new ArgumentException($"Expected {nameof(FlakAction)}.", nameof(action));
}
