using GrimSpace.Battle;
using GrimSpace.Battle.Effects;
using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.Statuses;
using GrimSpace.Battle.Targeting;
using GrimSpace.Battle.World;
using GrimSpace.Core.Actions;
using GrimSpace.Units.Loadouts.Abilities;

namespace GrimSpace.Battle.Actions;

public sealed record ProtectionDroneAction(string ActorId, string TargetId)
	: IAction<BattleWorld, ActorRuntime>
{
	public IActionDef<IAction, BattleWorld, ActorRuntime, IEffect<BattleWorld, ActorRuntime>> Definition =>
		ProtectionDroneDef.Instance;
}

public sealed class ProtectionDroneDef
	: IActionDef<IAction, BattleWorld, ActorRuntime, IEffect<BattleWorld, ActorRuntime>>,
		ITargetedActionDef
{
	public static ProtectionDroneDef Instance { get; } = new();

	public static TargetPickSpec TargetPick { get; } = new(
		1,
		1,
		[new RelationFilter(EUnitRelation.Ally), new ExcludeAlreadyPickedFilter()]);

	public bool IsBlockable => false;

	public IEnumerable<IAction> Discover(BattleWorld world, ActorRuntime runtime, string actorId)
	{
		if (world.StateOf(actorId).FindInstalled(EAbilityKind.ProtectionDrone) is null)
			yield break;

		foreach (var targetId in TargetPick.LegalTargets(world, actorId, Array.Empty<string>()))
		{
			var action = Bind(actorId, targetId);
			if (IsLegal(action, world, runtime))
				yield return action;
		}
	}

	public ProtectionDroneAction Bind(string actorId, string targetId) => new(actorId, targetId);

	IAction ITargetedActionDef.Bind(string actorId, IReadOnlyList<string> pickedIds)
	{
		if (pickedIds.Count != 1)
			throw new ArgumentException("Protection Drone requires exactly one ally target.", nameof(pickedIds));
		return Bind(actorId, pickedIds[0]);
	}

	public bool IsPossible(IAction action, BattleWorld world, ActorRuntime runtime) =>
		IsPossible(Cast(action), world, runtime);

	public bool IsLegal(IAction action, BattleWorld world, ActorRuntime runtime) =>
		IsLegal(Cast(action), world, runtime);

	public IReadOnlyList<IEffect<BattleWorld, ActorRuntime>> Resolve(
		IAction action,
		BattleWorld world,
		ActorRuntime runtime) =>
		Resolve(Cast(action), world, runtime);

	public bool IsPossible(ProtectionDroneAction action, BattleWorld world, ActorRuntime runtime) =>
		world.StateOf(action.ActorId).FindInstalled(EAbilityKind.ProtectionDrone) is not null;

	public bool IsLegal(ProtectionDroneAction action, BattleWorld world, ActorRuntime runtime)
	{
		if (!IsPossible(action, world, runtime))
			return false;

		var actor = world.StateOf(action.ActorId);
		var installed = actor.FindInstalled(EAbilityKind.ProtectionDrone);
		if (BattleStatuses.HasOverheat(actor)
			|| installed is null
			|| actor.CooldownRemaining(installed.Mount) > 0)
			return false;

		if (!TargetPickSpec.IsLiving(world, action.TargetId))
			return false;

		var target = world.StateOf(action.TargetId);
		if (target.Statuses.Any(status =>
			status.InstanceId
				== BattleStatuses.BuffInstanceId(nameof(EAbilityKind.ProtectionDrone), target.Id)))
			return false;

		return TargetPick.Filters.All(filter =>
			filter.Matches(world, action.ActorId, action.TargetId, Array.Empty<string>()));
	}

	public IReadOnlyList<IEffect<BattleWorld, ActorRuntime>> Resolve(
		ProtectionDroneAction action,
		BattleWorld world,
		ActorRuntime runtime)
	{
		var installed = world.StateOf(action.ActorId).FindInstalled(EAbilityKind.ProtectionDrone)
			?? throw new InvalidOperationException("Protection Drone is not installed.");
		if (installed.Spec is not ProtectionDroneSpec spec)
			throw new InvalidOperationException("Protection Drone spec missing.");

		var targetId = action.TargetId;
		return
		[
			new HeatChangeEffect(HeatChangeEffect.PerAbilityUse),
			new SetProtectionDroneBuffActiveEffect(targetId, active: true),
			new ScheduleActivationRoundEffect(
				EUnitActivationPhase.End,
				spec.BuffDurationEndPasses,
				sticky: false,
				new ClearProtectionDroneBuffAction(targetId),
				onUnitId: targetId),
			new MountCooldownEffect(installed.Mount, spec.CooldownTurns),
		];
	}

	private static ProtectionDroneAction Cast(IAction action) =>
		action is ProtectionDroneAction typed
			? typed
			: throw new ArgumentException($"Expected {nameof(ProtectionDroneAction)}.", nameof(action));
}
