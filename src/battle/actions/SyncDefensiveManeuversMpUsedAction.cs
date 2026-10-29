using GrimSpace.Battle.Effects;
using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.World;
using GrimSpace.Core.Actions;

namespace GrimSpace.Battle.Actions;

public sealed record SyncDefensiveManeuversMpUsedAction(string ActorId) : IAction<BattleWorld, ActorRuntime>
{
	public IActionDef<IAction, BattleWorld, ActorRuntime, IEffect<BattleWorld, ActorRuntime>> Definition =>
		SyncDefensiveManeuversMpUsedDef.Instance;
}

public sealed class SyncDefensiveManeuversMpUsedDef
	: IActionDef<IAction, BattleWorld, ActorRuntime, IEffect<BattleWorld, ActorRuntime>>
{
	public static SyncDefensiveManeuversMpUsedDef Instance { get; } = new();

	public IEnumerable<IAction> Discover(BattleWorld world, ActorRuntime runtime, string actorId) => [];

	public SyncDefensiveManeuversMpUsedAction Bind(string actorId) => new(actorId);

	public bool IsPossible(IAction action, BattleWorld world, ActorRuntime runtime) => true;

	public bool IsLegal(IAction action, BattleWorld world, ActorRuntime runtime) =>
		world.StateOf(Cast(action).ActorId).DefensiveManeuversActive;

	public IReadOnlyList<IEffect<BattleWorld, ActorRuntime>> Resolve(
		IAction action,
		BattleWorld world,
		ActorRuntime runtime) =>
		[new SyncDefensiveManeuversMpUsedFromPoolEffect()];

	private static SyncDefensiveManeuversMpUsedAction Cast(IAction action) =>
		action as SyncDefensiveManeuversMpUsedAction
		?? throw new ArgumentException($"Expected {nameof(SyncDefensiveManeuversMpUsedAction)}.", nameof(action));
}
