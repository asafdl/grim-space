using GrimSpace.Battle.Board;
using GrimSpace.Battle.Effects;
using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.Weapons;
using GrimSpace.Core.Actions;

namespace GrimSpace.Battle.Actions;

public sealed record RailgunAction(
	string OwnerId,
	string TargetUnitId,
	int? UndoGroup = null) : IAction<BattleBoard, ActorSession>
{
	public IActionDef<IAction, BattleBoard, ActorSession, IEffect<BattleBoard, ActorSession>> Definition =>
		RailgunDef.Instance;
}

public sealed class RailgunDef
	: IActionDef<IAction, BattleBoard, ActorSession, IEffect<BattleBoard, ActorSession>>
{
	public static RailgunDef Instance { get; } = new();

	public IEnumerable<IAction> Discover(BattleBoard world, ActorSession runtime, string ownerId)
	{
		foreach (var (unitId, unit) in world.Units)
		{
			if (unitId == ownerId || !unit.State.IsAlive)
				continue;

			var action = Bind(ownerId, unitId);
			if (IsPossible(action, world, runtime))
				yield return action;
		}
	}

	public RailgunAction Bind(string ownerId, string targetUnitId) => new(ownerId, targetUnitId);

	public bool IsPossible(IAction action, BattleBoard world, ActorSession runtime) =>
		IsPossible(Cast(action), world, runtime);

	public bool IsLegal(IAction action, BattleBoard world, ActorSession runtime) =>
		IsLegal(Cast(action), world, runtime);

	public IReadOnlyList<IEffect<BattleBoard, ActorSession>> Resolve(
		IAction action,
		BattleBoard world,
		ActorSession runtime) =>
		Resolve(Cast(action), world, runtime);

	public bool IsPossible(RailgunAction action, BattleBoard world, ActorSession runtime) =>
		IsLegal(action, world, runtime);

	public bool IsLegal(RailgunAction action, BattleBoard world, ActorSession runtime)
	{
		if (!world.Units.TryGetValue(action.TargetUnitId, out var targetUnit) || !targetUnit.State.IsAlive)
			return false;

		var target = targetUnit.State;
		if (target.MomentumLevel != CombatConfig.RailgunRequiredTargetMomentum)
			return false;

		var actor = world.StateOf(action.OwnerId);
		return actor.Position.ManhattanDistanceTo(target.Position) <= CombatConfig.RailgunMaxRange;
	}

	public IReadOnlyList<IEffect<BattleBoard, ActorSession>> Resolve(
		RailgunAction action,
		BattleBoard world,
		ActorSession runtime) =>
		[new DamageEffect(action.TargetUnitId, CombatConfig.RailgunDamage)];

	private static RailgunAction Cast(IAction action) =>
		action as RailgunAction ?? throw new ArgumentException($"Expected {nameof(RailgunAction)}.", nameof(action));
}
