using GrimSpace.Battle.World;
using GrimSpace.Battle.Effects;
using GrimSpace.Battle.Movement.Enums;
using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.Weapons;
using GrimSpace.Core.Actions;

namespace GrimSpace.Battle.Actions;

public sealed record RollAction(
	string ActorId,
	ERollDirection Direction) : IAction<BattleWorld, ActorRuntime>
{
	public IActionDef<IAction, BattleWorld, ActorRuntime, IEffect<BattleWorld, ActorRuntime>> Definition =>
		RollDef.Instance;
}

public sealed class RollDef
	: IActionDef<IAction, BattleWorld, ActorRuntime, IEffect<BattleWorld, ActorRuntime>>
{
	public static RollDef Instance { get; } = new();

	public IEnumerable<IAction> Discover(BattleWorld world, ActorRuntime runtime, string actorId)
	{
		foreach (var direction in Enum.GetValues<ERollDirection>())
		{
			var action = Bind(actorId, direction);
			if (IsPossible(action, world, runtime))
				yield return action;
		}
	}

	public RollAction Bind(string actorId, ERollDirection direction) => new(actorId, direction);

	public bool IsPossible(IAction action, BattleWorld world, ActorRuntime runtime) => true;

	public bool IsLegal(IAction action, BattleWorld world, ActorRuntime runtime) =>
		IsLegal(Cast(action), world, runtime);

	public IReadOnlyList<IEffect<BattleWorld, ActorRuntime>> Resolve(
		IAction action,
		BattleWorld world,
		ActorRuntime runtime) =>
		Resolve(Cast(action), world, runtime);

	public bool IsLegal(RollAction action, BattleWorld world, ActorRuntime runtime) =>
		world.StateOf(action.ActorId).ActionPoints >= CombatConfig.RollApCost;

	public IReadOnlyList<IEffect<BattleWorld, ActorRuntime>> Resolve(
		RollAction action,
		BattleWorld world,
		ActorRuntime runtime) =>
	[
		new RollEffect(action.Direction),
		new ApChangeEffect(-CombatConfig.RollApCost),
	];

	private static RollAction Cast(IAction action) =>
		action as RollAction ?? throw new ArgumentException($"Expected {nameof(RollAction)}.", nameof(action));
}
