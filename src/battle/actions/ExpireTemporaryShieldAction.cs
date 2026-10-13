using GrimSpace.Battle.Effects;
using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.World;
using GrimSpace.Core.Actions;

namespace GrimSpace.Battle.Actions;

public sealed record ExpireTemporaryShieldGrantAction(string ActorId, string GrantId)
	: IAction<BattleWorld, ActorRuntime>
{
	public IActionDef<IAction, BattleWorld, ActorRuntime, IEffect<BattleWorld, ActorRuntime>> Definition =>
		ExpireTemporaryShieldGrantDef.Instance;
}

public sealed class ExpireTemporaryShieldGrantDef
	: IActionDef<IAction, BattleWorld, ActorRuntime, IEffect<BattleWorld, ActorRuntime>>
{
	public static ExpireTemporaryShieldGrantDef Instance { get; } = new();

	public IEnumerable<IAction> Discover(BattleWorld world, ActorRuntime runtime, string actorId) => [];

	public ExpireTemporaryShieldGrantAction Bind(string actorId, string grantId) => new(actorId, grantId);

	public bool IsPossible(IAction action, BattleWorld world, ActorRuntime runtime) => true;

	public bool IsLegal(IAction action, BattleWorld world, ActorRuntime runtime) => true;

	public IReadOnlyList<IEffect<BattleWorld, ActorRuntime>> Resolve(
		IAction action,
		BattleWorld world,
		ActorRuntime runtime) =>
		action is ExpireTemporaryShieldGrantAction typed
			? [new RevokeTemporaryShieldGrantEffect(typed.GrantId)]
			: [];
}
