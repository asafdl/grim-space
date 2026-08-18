using GrimSpace.Battle.Effects;
using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.Units;
using GrimSpace.Battle.Abilities;
using GrimSpace.Battle.World;
using GrimSpace.Core.Actions;
using GrimSpace.Units.Enums;

namespace GrimSpace.Battle.Actions;

public sealed record SpawnPatrolAction(string ActorId, string? SpawnedUnitId = null)
	: IAction<BattleWorld, ActorRuntime>
{
	public IActionDef<IAction, BattleWorld, ActorRuntime, IEffect<BattleWorld, ActorRuntime>> Definition =>
		SpawnPatrolDef.Instance;
}

public sealed class SpawnPatrolDef
	: IActionDef<IAction, BattleWorld, ActorRuntime, IEffect<BattleWorld, ActorRuntime>>,
		IActorActionDef
{
	public static SpawnPatrolDef Instance { get; } = new();

	public IEnumerable<IAction> Discover(BattleWorld world, ActorRuntime runtime, string actorId)
	{
		var action = Bind(actorId);
		if (IsLegal(action, world, runtime))
			yield return action;
	}

	public SpawnPatrolAction Bind(string actorId) => new(actorId);

	IAction IActorActionDef.Bind(string actorId) => Bind(actorId);

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
		var actor = world.StateOf(action.ActorId);
		var (position, _, _) = PatrolBayMount.LaunchPose(actor);
		return world.Grid.IsInBounds(position) && !world.BlockedFor(action.ActorId).Contains(position);
	}

	public bool IsLegal(SpawnPatrolAction action, BattleWorld world, ActorRuntime runtime)
	{
		var actor = world.StateOf(action.ActorId);
		if (actor.PatrolSpawnCooldownRemaining > 0)
			return false;
		if (LivingPatrolChildren(world, action.ActorId) >= CombatConfig.MaxLivingPatrolChildren)
			return false;

		return IsPossible(action, world, runtime);
	}

	public IReadOnlyList<IEffect<BattleWorld, ActorRuntime>> Resolve(
		SpawnPatrolAction action,
		BattleWorld world,
		ActorRuntime runtime) =>
	[
		new SpawnPatrolEffect(action.SpawnedUnitId),
		new PatrolSpawnCooldownEffect(CombatConfig.PatrolCooldownTurns),
	];

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
