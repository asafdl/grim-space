using GrimSpace.Battle.World;
using GrimSpace.Battle.Effects;
using GrimSpace.Battle.Ids;
using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.Units;
using GrimSpace.Battle.Abilities;
using GrimSpace.Core.Actions;
using GrimSpace.Core.Ids;
using GrimSpace.Math.Grid;
using GrimSpace.Units;
using GrimSpace.Units.Enums;
using GrimSpace.Units.Loadouts.Abilities;

namespace GrimSpace.Battle.Actions;

public sealed record VoidBombAction(
	string ActorId,
	ESpatialOrientation MountedOn,
	string SpawnedUnitId) : IAction<BattleWorld, ActorRuntime>, IMountedAction
{
	public IActionDef<IAction, BattleWorld, ActorRuntime, IEffect<BattleWorld, ActorRuntime>> Definition =>
		VoidBombDef.Instance;
}

public sealed class VoidBombDef
	: IActionDef<IAction, BattleWorld, ActorRuntime, IEffect<BattleWorld, ActorRuntime>>,
		IMountedActionDef
{
	public static VoidBombDef Instance { get; } = new();

	public IEnumerable<IAction> Discover(BattleWorld world, ActorRuntime runtime, string actorId)
	{
		var spawnedUnitId = TypedIdGenerator.NextId(UnitTypeSlug.For(EType.VoidBomb));
		foreach (var action in Discover(actorId, spawnedUnitId, world))
		{
			if (IsPossible(action, world, runtime))
				yield return action;
		}
	}

	internal IEnumerable<VoidBombAction> Discover(string actorId, string spawnedUnitId, BattleWorld world)
	{
		var state = world.StateOf(actorId);
		foreach (var installed in state.Loadout.InstalledAbilities)
		{
			if (installed.Spec.Kind != EAbilityKind.VoidBombLauncher)
				continue;

			yield return Bind(actorId, installed.MountedOn, spawnedUnitId);
		}
	}

	public VoidBombAction Bind(string actorId, ESpatialOrientation mountedOn) =>
		Bind(actorId, mountedOn, TypedIdGenerator.NextId(UnitTypeSlug.For(EType.VoidBomb)));

	public VoidBombAction Bind(string actorId, ESpatialOrientation mountedOn, string spawnedUnitId) =>
		new(actorId, mountedOn, spawnedUnitId);

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

	public bool IsPossible(VoidBombAction action, BattleWorld world, ActorRuntime runtime)
	{
		if (string.IsNullOrWhiteSpace(action.SpawnedUnitId))
			return false;

		var installed = world.StateOf(action.ActorId).FindInstalled(
			EAbilityKind.VoidBombLauncher,
			action.MountedOn);
		if (installed is null)
			return false;

		var ship = world.StateOf(action.ActorId);
		var (position, _, _) = VoidBombMount.LaunchPose(ship, action.MountedOn);
		return world.Grid.IsInBounds(position) && !world.IsCellBlocked(position);
	}

	public bool IsLegal(VoidBombAction action, BattleWorld world, ActorRuntime runtime)
	{
		var state = world.StateOf(action.ActorId);
		var installed = state.FindInstalled(EAbilityKind.VoidBombLauncher, action.MountedOn);
		if (installed is null)
			return false;
		if (state.MountRuntimeFor(installed.Mount).CooldownRemaining > 0)
			return false;

		return IsPossible(action, world, runtime);
	}

	public IReadOnlyList<IEffect<BattleWorld, ActorRuntime>> Resolve(
		VoidBombAction action,
		BattleWorld world,
		ActorRuntime runtime)
	{
		var state = world.StateOf(action.ActorId);
		var installed = state.FindInstalled(EAbilityKind.VoidBombLauncher, action.MountedOn)
			?? throw new InvalidOperationException("VoidBomb launcher not installed for actor.");
		var cooldown = installed.Spec is ICooldownAbility cooldownAbility
			? cooldownAbility.CooldownTurns
			: throw new InvalidOperationException("VoidBomb launcher spec missing cooldown.");
		return
		[
			new SpawnVoidBombEffect(installed.Mount, action.SpawnedUnitId),
			new MountCooldownEffect(installed.Mount, cooldown),
		];
	}

	private static VoidBombAction Cast(IAction action) =>
		action as VoidBombAction ?? throw new ArgumentException($"Expected {nameof(VoidBombAction)}.", nameof(action));
}
