using GrimSpace.Battle.Effects;
using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.World;
using GrimSpace.Core.Actions;

namespace GrimSpace.Battle.Actions;

public sealed record ClearProtectionDroneBuffAction(string ActorId) : IAction<BattleWorld, ActorRuntime>
{
	public IActionDef<IAction, BattleWorld, ActorRuntime, IEffect<BattleWorld, ActorRuntime>> Definition =>
		ClearProtectionDroneBuffDef.Instance;
}

public sealed class ClearProtectionDroneBuffDef
	: IActionDef<IAction, BattleWorld, ActorRuntime, IEffect<BattleWorld, ActorRuntime>>
{
	public static ClearProtectionDroneBuffDef Instance { get; } = new();

	public IEnumerable<IAction> Discover(BattleWorld world, ActorRuntime runtime, string actorId) => [];

	public ClearProtectionDroneBuffAction Bind(string actorId) => new(actorId);

	public bool IsPossible(IAction action, BattleWorld world, ActorRuntime runtime) => true;

	public bool IsLegal(IAction action, BattleWorld world, ActorRuntime runtime) => true;

	public IReadOnlyList<IEffect<BattleWorld, ActorRuntime>> Resolve(
		IAction action,
		BattleWorld world,
		ActorRuntime runtime) =>
		[new SetProtectionDroneBuffActiveEffect(Cast(action).ActorId, active: false)];

	private static ClearProtectionDroneBuffAction Cast(IAction action) =>
		action is ClearProtectionDroneBuffAction typed
			? typed
			: throw new ArgumentException($"Expected {nameof(ClearProtectionDroneBuffAction)}.", nameof(action));
}
