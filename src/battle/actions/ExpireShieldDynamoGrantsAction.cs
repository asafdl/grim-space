using GrimSpace.Battle.Effects;
using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.Statuses;
using GrimSpace.Battle.World;
using GrimSpace.Core.Actions;

namespace GrimSpace.Battle.Actions;

public sealed record ExpireShieldDynamoGrantsAction(string ActorId) : IAction<BattleWorld, ActorRuntime>
{
	public IActionDef<IAction, BattleWorld, ActorRuntime, IEffect<BattleWorld, ActorRuntime>> Definition =>
		ExpireShieldDynamoGrantsDef.Instance;
}

public sealed class ExpireShieldDynamoGrantsDef
	: IActionDef<IAction, BattleWorld, ActorRuntime, IEffect<BattleWorld, ActorRuntime>>
{
	public static ExpireShieldDynamoGrantsDef Instance { get; } = new();

	public IEnumerable<IAction> Discover(BattleWorld world, ActorRuntime runtime, string actorId) => [];

	public ExpireShieldDynamoGrantsAction Bind(string actorId) => new(actorId);

	public bool IsPossible(IAction action, BattleWorld world, ActorRuntime runtime) => true;

	public bool IsLegal(IAction action, BattleWorld world, ActorRuntime runtime) =>
		BattleStatuses.HasShieldDynamo(world.StateOf(Cast(action).ActorId));

	public IReadOnlyList<IEffect<BattleWorld, ActorRuntime>> Resolve(
		IAction action,
		BattleWorld world,
		ActorRuntime runtime) =>
		[new ExpireShieldDynamoGrantsEffect()];

	private static ExpireShieldDynamoGrantsAction Cast(IAction action) =>
		action as ExpireShieldDynamoGrantsAction
		?? throw new ArgumentException($"Expected {nameof(ExpireShieldDynamoGrantsAction)}.", nameof(action));
}
