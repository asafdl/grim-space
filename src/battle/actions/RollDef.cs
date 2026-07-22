using GrimSpace.Battle.Board;
using GrimSpace.Battle.Effects;
using GrimSpace.Battle.Movement.Enums;
using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.Weapons;
using GrimSpace.Core.Actions;

namespace GrimSpace.Battle.Actions;

public sealed class RollDef
	: IActionDef<IAction, BattleBoard, ActorSession, IEffect<BattleBoard, ActorSession>>
{
	public static RollDef Instance { get; } = new();

	public IEnumerable<IAction> Discover(BattleBoard world, ActorSession runtime, string ownerId)
	{
		foreach (var direction in Enum.GetValues<ERollDirection>())
		{
			var action = new RollAction(ownerId, direction);
			if (IsPossible(action, world, runtime))
				yield return action;
		}
	}

	public bool IsPossible(IAction action, BattleBoard world, ActorSession runtime) => true;

	public bool IsLegal(IAction action, BattleBoard world, ActorSession runtime) =>
		IsLegal(Cast(action), world, runtime);

	public IReadOnlyList<IEffect<BattleBoard, ActorSession>> Resolve(
		IAction action,
		BattleBoard world,
		ActorSession runtime) =>
		Resolve(Cast(action), world, runtime);

	public bool IsLegal(RollAction action, BattleBoard world, ActorSession runtime) =>
		world.StateOf(action.OwnerId).ActionPoints >= CombatConfig.RollApCost;

	public IReadOnlyList<IEffect<BattleBoard, ActorSession>> Resolve(
		RollAction action,
		BattleBoard world,
		ActorSession runtime) =>
	[
		new RollEffect(action.Direction),
		new ApChangeEffect(-CombatConfig.RollApCost),
	];

	private static RollAction Cast(IAction action) =>
		action as RollAction ?? throw new ArgumentException($"Expected {nameof(RollAction)}.", nameof(action));
}
