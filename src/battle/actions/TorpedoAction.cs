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

public sealed record TorpedoAction(
	string ActorId,
	ESpatialOrientation MountedOn,
	string SpawnedUnitId) : IAction<BattleWorld, ActorRuntime>, IMountedAction
{
	public IActionDef<IAction, BattleWorld, ActorRuntime, IEffect<BattleWorld, ActorRuntime>> Definition =>
		TorpedoDef.Instance;
}

public sealed class TorpedoDef
	: IActionDef<IAction, BattleWorld, ActorRuntime, IEffect<BattleWorld, ActorRuntime>>,
		IMountedActionDef
{
	public static TorpedoDef Instance { get; } = new();

	public IEnumerable<IAction> Discover(BattleWorld world, ActorRuntime runtime, string actorId)
	{
		var spawnedUnitId = TypedIdGenerator.NextId(UnitTypeSlug.For(EType.Torpedo));
		foreach (var action in Discover(actorId, spawnedUnitId, world))
		{
			if (IsPossible(action, world, runtime))
				yield return action;
		}
	}

	internal IEnumerable<TorpedoAction> Discover(string actorId, string spawnedUnitId, BattleWorld world)
	{
		var state = world.StateOf(actorId);
		foreach (var installed in state.Spec.InstalledAbilities)
		{
			if (installed.Spec.Kind != EAbilityKind.TorpedoLauncher)
				continue;

			foreach (var mountedOn in installed.ForAction().Facets)
				yield return Bind(actorId, mountedOn, spawnedUnitId);
		}
	}

	public TorpedoAction Bind(string actorId, ESpatialOrientation mountedOn) =>
		Bind(actorId, mountedOn, TypedIdGenerator.NextId(UnitTypeSlug.For(EType.Torpedo)));

	public TorpedoAction Bind(string actorId, ESpatialOrientation mountedOn, string spawnedUnitId) =>
		new(actorId, mountedOn, spawnedUnitId);

	public bool SupportsMount(ESpatialOrientation mountedOn) => true;

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

	public bool IsPossible(TorpedoAction action, BattleWorld world, ActorRuntime runtime)
	{
		if (string.IsNullOrWhiteSpace(action.SpawnedUnitId))
			return false;

		var installed = world.StateOf(action.ActorId).FindInstalled(
			EAbilityKind.TorpedoLauncher,
			action.MountedOn);
		if (installed is null || !installed.Facets.Contains(action.MountedOn))
			return false;

		var ship = world.StateOf(action.ActorId);
		var (position, _, _) = TorpedoMount.LaunchPose(ship, action.MountedOn);
		return world.Grid.IsInBounds(position) && !world.BlockedFor(action.ActorId).Contains(position);
	}

	public bool IsLegal(TorpedoAction action, BattleWorld world, ActorRuntime runtime)
	{
		var state = world.StateOf(action.ActorId);
		var installed = state.FindInstalled(EAbilityKind.TorpedoLauncher, action.MountedOn);
		if (installed is null)
			return false;
		if (state.MountRuntimeFor(EAbilityKind.TorpedoLauncher).CooldownRemaining > 0)
			return false;

		return IsPossible(action, world, runtime);
	}

	public IReadOnlyList<IEffect<BattleWorld, ActorRuntime>> Resolve(
		TorpedoAction action,
		BattleWorld world,
		ActorRuntime runtime)
	{
		var state = world.StateOf(action.ActorId);
		var installed = state.FindInstalled(EAbilityKind.TorpedoLauncher, action.MountedOn)
			?? throw new InvalidOperationException("Torpedo launcher not installed for actor.");
		var cooldown = installed.Spec is ICooldownAbility cooldownAbility
			? cooldownAbility.CooldownTurns
			: throw new InvalidOperationException("Torpedo launcher spec missing cooldown.");
		return
		[
			new SpawnTorpedoEffect(action.MountedOn, action.SpawnedUnitId),
			new MountCooldownEffect(EAbilityKind.TorpedoLauncher, cooldown),
		];
	}

	private static TorpedoAction Cast(IAction action) =>
		action as TorpedoAction ?? throw new ArgumentException($"Expected {nameof(TorpedoAction)}.", nameof(action));
}
