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

public sealed record SpawnRepurposedMinerAction(
	string ActorId,
	ESpatialOrientation MountedOn,
	string SpawnedUnitId)
	: IAction<BattleWorld, ActorRuntime>, IMountedAction
{
	public IActionDef<IAction, BattleWorld, ActorRuntime, IEffect<BattleWorld, ActorRuntime>> Definition =>
		SpawnRepurposedMinerDef.Instance;
}

public sealed class SpawnRepurposedMinerDef
	: IActionDef<IAction, BattleWorld, ActorRuntime, IEffect<BattleWorld, ActorRuntime>>,
		IMountedActionDef
{
	public static SpawnRepurposedMinerDef Instance { get; } = new();

	public IEnumerable<IAction> Discover(BattleWorld world, ActorRuntime runtime, string actorId)
	{
		foreach (var installed in world.StateOf(actorId).Loadout.InstalledAbilities)
		{
			if (installed.Spec.Kind != EAbilityKind.MinerBay)
				continue;

			var action = Bind(actorId, installed.MountedOn);
			if (IsLegal(action, world, runtime))
				yield return action;
		}
	}

	public SpawnRepurposedMinerAction Bind(string actorId, ESpatialOrientation mountedOn) =>
		new(actorId, mountedOn, TypedIdGenerator.NextId(UnitTypeSlug.For(EType.RepurposedMiner)));

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

	public bool IsPossible(SpawnRepurposedMinerAction action, BattleWorld world, ActorRuntime runtime)
	{
		if (string.IsNullOrWhiteSpace(action.SpawnedUnitId))
			return false;

		var actor = world.StateOf(action.ActorId);
		if (actor.FindInstalled(EAbilityKind.MinerBay, action.MountedOn) is null)
			return false;

		var (position, _, _) = MinerBayMount.LaunchPose(actor, action.MountedOn);
		return world.Grid.IsInBounds(position) && !world.IsCellBlocked(position);
	}

	public bool IsLegal(SpawnRepurposedMinerAction action, BattleWorld world, ActorRuntime runtime)
	{
		var actor = world.StateOf(action.ActorId);
		var installed = actor.FindInstalled(EAbilityKind.MinerBay, action.MountedOn);
		if (installed is null)
			return false;
		if (actor.CooldownRemaining(installed.Mount) > 0)
			return false;
		if (installed.Spec is not ISpawnable spawnable)
			return false;
		if (LivingRepurposedMinerChildren(world, action.ActorId) >= spawnable.MaxLivingChildren)
			return false;

		return IsPossible(action, world, runtime);
	}

	public IReadOnlyList<IEffect<BattleWorld, ActorRuntime>> Resolve(
		SpawnRepurposedMinerAction action,
		BattleWorld world,
		ActorRuntime runtime)
	{
		var state = world.StateOf(action.ActorId);
		var installed = state.FindInstalled(EAbilityKind.MinerBay, action.MountedOn)
			?? throw new InvalidOperationException("Repurposed Miner bay not installed for actor.");
		var cooldown = installed.Spec is ICooldownAbility cooldownAbility
			? cooldownAbility.CooldownTurns
			: throw new InvalidOperationException("Repurposed Miner bay spec missing cooldown.");
		return
		[
			new SpawnRepurposedMinerEffect(installed.Mount, action.SpawnedUnitId),
			new MountCooldownEffect(installed.Mount, cooldown),
		];
	}

	private static int LivingRepurposedMinerChildren(BattleWorld world, string parentId)
	{
		var count = 0;
		foreach (var unit in UnitRegistry.For(world).All)
		{
			if (unit.State.ParentId != parentId
				|| unit.State.Type != EType.RepurposedMiner
				|| !unit.State.IsAlive)
			{
				continue;
			}

			count++;
		}

		return count;
	}

	private static SpawnRepurposedMinerAction Cast(IAction action) =>
		action as SpawnRepurposedMinerAction ?? throw new ArgumentException($"Expected {nameof(SpawnRepurposedMinerAction)}.", nameof(action));
}
