using GrimSpace.Battle.World;
using GrimSpace.Battle.Effects;
using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.Weapons;
using GrimSpace.Core.Actions;

namespace GrimSpace.Battle.Actions;

public sealed record RailgunAction(
	string ActorId,
	string TargetUnitId) : IAction<BattleWorld, ActorRuntime>
{
	public IActionDef<IAction, BattleWorld, ActorRuntime, IEffect<BattleWorld, ActorRuntime>> Definition =>
		RailgunDef.Instance;
}

public sealed class RailgunDef
	: IActionDef<IAction, BattleWorld, ActorRuntime, IEffect<BattleWorld, ActorRuntime>>
{
	public static RailgunDef Instance { get; } = new();

	public IEnumerable<IAction> Discover(BattleWorld world, ActorRuntime runtime, string actorId)
	{
		foreach (var (unitId, unit) in world.Units)
		{
			if (unitId == actorId || !unit.State.IsAlive)
				continue;

			var action = Bind(actorId, unitId);
			if (IsPossible(action, world, runtime))
				yield return action;
		}
	}

	public RailgunAction Bind(string actorId, string targetUnitId) => new(actorId, targetUnitId);

	public bool IsPossible(IAction action, BattleWorld world, ActorRuntime runtime) =>
		IsPossible(Cast(action), world, runtime);

	public bool IsLegal(IAction action, BattleWorld world, ActorRuntime runtime) =>
		IsLegal(Cast(action), world, runtime);

	public IReadOnlyList<IEffect<BattleWorld, ActorRuntime>> Resolve(
		IAction action,
		BattleWorld world,
		ActorRuntime runtime) =>
		Resolve(Cast(action), world, runtime);

	public bool IsPossible(RailgunAction action, BattleWorld world, ActorRuntime runtime) =>
		IsLegal(action, world, runtime);

	public bool IsLegal(RailgunAction action, BattleWorld world, ActorRuntime runtime)
	{
		if (world.StateOf(action.ActorId).RailgunRemaining <= 0)
			return false;

		if (!world.Units.TryGetValue(action.TargetUnitId, out var targetUnit) || !targetUnit.State.IsAlive)
			return false;

		var target = targetUnit.State;
		if (target.MomentumLevel != CombatConfig.RailgunRequiredTargetMomentum)
			return false;

		var actor = world.StateOf(action.ActorId);
		return actor.Position.ManhattanDistanceTo(target.Position) <= CombatConfig.RailgunMaxRange;
	}

	public IReadOnlyList<IEffect<BattleWorld, ActorRuntime>> Resolve(
		RailgunAction action,
		BattleWorld world,
		ActorRuntime runtime) =>
		[
			new DamageEffect(action.TargetUnitId, CombatConfig.RailgunDamage),
			new RailgunChangeEffect(-1),
		];

	private static RailgunAction Cast(IAction action) =>
		action as RailgunAction ?? throw new ArgumentException($"Expected {nameof(RailgunAction)}.", nameof(action));
}
