using GrimSpace.Battle.Effects;
using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.Units;
using GrimSpace.Battle.Abilities;
using GrimSpace.Battle.World;
using GrimSpace.Core.Actions;
using GrimSpace.Battle.Ids;
using GrimSpace.Core.Ids;
using GrimSpace.Math.Grid;
using GrimSpace.Units;
using GrimSpace.Units.Enums;
using GrimSpace.Units.Loadouts.Abilities;

namespace GrimSpace.Battle.Actions;

public sealed record SpawnPatrolAction(
	string ActorId,
	ESpatialOrientation MountedOn,
	string SpawnedUnitId)
	: IAction<BattleWorld, ActorRuntime>, IMountedAction
{
	public IActionDef<IAction, BattleWorld, ActorRuntime, IEffect<BattleWorld, ActorRuntime>> Definition =>
		SpawnPatrolDef.Instance;
}

public sealed class SpawnPatrolDef
	: IActionDef<IAction, BattleWorld, ActorRuntime, IEffect<BattleWorld, ActorRuntime>>,
		IMountedActionDef
{
	public static SpawnPatrolDef Instance { get; } = new();

	public IEnumerable<IAction> Discover(BattleWorld world, ActorRuntime runtime, string actorId)
	{
		foreach (var installed in world.StateOf(actorId).Spec.InstalledAbilities)
		{
			if (installed.Spec.Kind != EAbilityKind.PatrolBay)
				continue;

			var action = Bind(actorId, installed.MountedOn);
			if (IsLegal(action, world, runtime))
				yield return action;
		}
	}

	public SpawnPatrolAction Bind(string actorId, ESpatialOrientation mountedOn) =>
		new(actorId, mountedOn, TypedIdGenerator.NextId(UnitTypeSlug.For(EType.Patrol)));

	IAction IMountedActionDef.Bind(string actorId, ESpatialOrientation mountedOn) =>
		Bind(actorId, mountedOn);

	public bool IsPossible(IAction action, BattleWorld world, ActorRuntime runtime) =>
		IsPossible(Cast(action), world, runtime);

	public bool IsLegal(IAction action, BattleWorld world, ActorRuntime runtime) =>
		IsLegal(Cast(action), world, runtime);

	public IReadOnlyList<IEffect<BattleWorld, ActorRuntime>> Resolve(
		IAction action,
		BattleWorld world,
		ActorRuntime runtime) =>
		Resolve(Cast(action), world, runtime);

	public bool IsPossible(SpawnPatrolAction action, BattleWorld world, ActorRuntime runtime)
	{
		if (string.IsNullOrWhiteSpace(action.SpawnedUnitId))
			return false;

		var actor = world.StateOf(action.ActorId);
		if (actor.FindInstalled(EAbilityKind.PatrolBay, action.MountedOn) is null)
			return false;

		var (position, _, _) = PatrolBayMount.LaunchPose(actor, action.MountedOn);
		return world.Grid.IsInBounds(position) && !world.BlockedFor(action.ActorId).Contains(position);
	}

	public bool IsLegal(SpawnPatrolAction action, BattleWorld world, ActorRuntime runtime)
	{
		var actor = world.StateOf(action.ActorId);
		var installed = actor.FindInstalled(EAbilityKind.PatrolBay, action.MountedOn);
		if (installed is null)
			return false;
		if (actor.CooldownRemaining(installed.Mount) > 0)
			return false;
		if (installed.Spec is not ISpawnable spawnable)
			return false;
		if (LivingPatrolChildren(world, action.ActorId) >= spawnable.MaxLivingChildren)
			return false;

		return IsPossible(action, world, runtime);
	}

	public IReadOnlyList<IEffect<BattleWorld, ActorRuntime>> Resolve(
		SpawnPatrolAction action,
		BattleWorld world,
		ActorRuntime runtime)
	{
		var state = world.StateOf(action.ActorId);
		var installed = state.FindInstalled(EAbilityKind.PatrolBay, action.MountedOn)
			?? throw new InvalidOperationException("Patrol bay not installed for actor.");
		var cooldown = installed.Spec is ICooldownAbility cooldownAbility
			? cooldownAbility.CooldownTurns
			: throw new InvalidOperationException("Patrol bay spec missing cooldown.");
		return
		[
			new SpawnPatrolEffect(installed.Mount, action.SpawnedUnitId),
			new MountCooldownEffect(installed.Mount, cooldown),
		];
	}

	private static int LivingPatrolChildren(BattleWorld world, string parentId)
	{
		var count = 0;
		foreach (var unit in UnitRegistry.For(world).All)
		{
			if (unit.State.ParentId != parentId
				|| unit.State.Type != EType.Patrol
				|| !unit.State.IsAlive)
			{
				continue;
			}

			count++;
		}

		return count;
	}

	private static SpawnPatrolAction Cast(IAction action) =>
		action as SpawnPatrolAction ?? throw new ArgumentException($"Expected {nameof(SpawnPatrolAction)}.", nameof(action));
}
