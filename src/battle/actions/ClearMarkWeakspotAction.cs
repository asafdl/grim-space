using GrimSpace.Battle.Effects;
using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.World;
using GrimSpace.Core.Actions;
using GrimSpace.Math.Grid;

namespace GrimSpace.Battle.Actions;

public sealed record ClearMarkWeakspotAction(string ActorId) : IAction<BattleWorld, ActorRuntime>
{
	public IActionDef<IAction, BattleWorld, ActorRuntime, IEffect<BattleWorld, ActorRuntime>> Definition =>
		ClearMarkWeakspotDef.Instance;
}

public sealed class ClearMarkWeakspotDef
	: IActionDef<IAction, BattleWorld, ActorRuntime, IEffect<BattleWorld, ActorRuntime>>
{
	public static ClearMarkWeakspotDef Instance { get; } = new();

	public IEnumerable<IAction> Discover(BattleWorld world, ActorRuntime runtime, string actorId) => [];

	public bool IsPossible(IAction action, BattleWorld world, ActorRuntime runtime) => true;

	public bool IsLegal(IAction action, BattleWorld world, ActorRuntime runtime) => true;

	public IReadOnlyList<IEffect<BattleWorld, ActorRuntime>> Resolve(
		IAction action,
		BattleWorld world,
		ActorRuntime runtime) =>
		[new SetMarkWeakspotDebuffEffect(Cast(action).ActorId, ESpatialOrientation.Forward, active: false)];

	private static ClearMarkWeakspotAction Cast(IAction action) =>
		action as ClearMarkWeakspotAction
		?? throw new ArgumentException($"Expected {nameof(ClearMarkWeakspotAction)}.", nameof(action));
}
