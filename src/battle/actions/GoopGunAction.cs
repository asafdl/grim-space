using GrimSpace.Battle.Effects;
using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.Units;
using GrimSpace.Battle.World;
using GrimSpace.Core.Actions;
using GrimSpace.Core.Ids;
using GrimSpace.Math.Grid;
using GrimSpace.Units.Loadouts.Abilities;

namespace GrimSpace.Battle.Actions;

public sealed record GoopGunAction(
	string ActorId,
	ESpatialOrientation MountedOn,
	string GoopHazardId) : IAction<BattleWorld, ActorRuntime>, IMountedAction
{
	public IActionDef<IAction, BattleWorld, ActorRuntime, IEffect<BattleWorld, ActorRuntime>> Definition =>
		GoopGunDef.Instance;
}

public sealed class GoopGunDef
	: IActionDef<IAction, BattleWorld, ActorRuntime, IEffect<BattleWorld, ActorRuntime>>,
		IMountedActionDef
{
	public static GoopGunDef Instance { get; } = new();

	public IEnumerable<IAction> Discover(BattleWorld world, ActorRuntime runtime, string actorId)
	{
		var goopHazardId = TypedIdGenerator.NextId(NonUnitTypeSlug.Goop);
		foreach (var action in Discover(actorId, goopHazardId, world))
		{
			if (IsPossible(action, world, runtime))
				yield return action;
		}
	}

	internal IEnumerable<GoopGunAction> Discover(string actorId, string goopHazardId, BattleWorld world)
	{
		var state = world.StateOf(actorId);
		foreach (var installed in state.Loadout.InstalledAbilities)
		{
			if (installed.Spec.Kind != EAbilityKind.GoopGun)
				continue;

			yield return Bind(actorId, installed.MountedOn, goopHazardId);
		}
	}

	public GoopGunAction Bind(string actorId, ESpatialOrientation mountedOn) =>
		Bind(actorId, mountedOn, TypedIdGenerator.NextId(NonUnitTypeSlug.Goop));

	public GoopGunAction Bind(string actorId, ESpatialOrientation mountedOn, string goopHazardId) =>
		new(actorId, mountedOn, goopHazardId);

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

	public bool IsPossible(GoopGunAction action, BattleWorld world, ActorRuntime runtime)
	{
		if (string.IsNullOrWhiteSpace(action.GoopHazardId))
			return false;

		return world.StateOf(action.ActorId).FindInstalled(EAbilityKind.GoopGun, action.MountedOn) is not null;
	}

	public bool IsLegal(GoopGunAction action, BattleWorld world, ActorRuntime runtime)
	{
		var state = world.StateOf(action.ActorId);
		var installed = state.FindInstalled(EAbilityKind.GoopGun, action.MountedOn);
		if (installed is null || state.MountRuntimeFor(installed.Mount).CooldownRemaining > 0)
			return false;

		return IsPossible(action, world, runtime);
	}

	public IReadOnlyList<IEffect<BattleWorld, ActorRuntime>> Resolve(
		GoopGunAction action,
		BattleWorld world,
		ActorRuntime runtime)
	{
		var state = world.StateOf(action.ActorId);
		var installed = state.FindInstalled(EAbilityKind.GoopGun, action.MountedOn)
			?? throw new InvalidOperationException("Goop gun not installed for actor.");
		var unavailableTurns = installed.Spec is GoopGunSpec goopGun
			? goopGun.UnavailableTurns
			: throw new InvalidOperationException("Goop gun spec missing unavailable turns.");
		return
		[
			new MountCooldownEffect(installed.Mount, unavailableTurns + 1),
		];
	}

	private static GoopGunAction Cast(IAction action) =>
		action as GoopGunAction ?? throw new ArgumentException($"Expected {nameof(GoopGunAction)}.", nameof(action));
}
