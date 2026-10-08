using GrimSpace.Battle.Effects;
using GrimSpace.Battle.Ids;
using GrimSpace.Battle.NonUnits;
using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.World;
using GrimSpace.Core.Actions;

namespace GrimSpace.Battle.Actions;

public sealed record RemoveHazardAction(string OwnerId, string HazardId) : IAction<BattleWorld, ActorRuntime>
{
	public string ActorId => BattleActorIds.Rules;

	public IActionDef<IAction, BattleWorld, ActorRuntime, IEffect<BattleWorld, ActorRuntime>> Definition =>
		RemoveHazardDef.Instance;
}

public sealed class RemoveHazardDef
	: IActionDef<IAction, BattleWorld, ActorRuntime, IEffect<BattleWorld, ActorRuntime>>
{
	public static RemoveHazardDef Instance { get; } = new();

	public IEnumerable<IAction> Discover(BattleWorld world, ActorRuntime runtime, string actorId) => [];

	public bool IsPossible(IAction action, BattleWorld world, ActorRuntime runtime) =>
		IsPossible(Cast(action), world);

	public bool IsLegal(IAction action, BattleWorld world, ActorRuntime runtime) =>
		IsLegal(Cast(action), world);

	public IReadOnlyList<IEffect<BattleWorld, ActorRuntime>> Resolve(
		IAction action,
		BattleWorld world,
		ActorRuntime runtime) =>
		Resolve(Cast(action));

	public bool IsPossible(RemoveHazardAction action, BattleWorld world) => true;

	public bool IsLegal(RemoveHazardAction action, BattleWorld world) =>
		world.NonUnits.TryGetValue(action.HazardId, out var nonUnit)
		&& nonUnit is Hazard hazard
		&& string.Equals(hazard.ActorId, action.OwnerId, StringComparison.Ordinal);

	public IReadOnlyList<IEffect<BattleWorld, ActorRuntime>> Resolve(RemoveHazardAction action) =>
		[new RemoveHazardEffect(action.HazardId, action.OwnerId)];

	private static RemoveHazardAction Cast(IAction action) =>
		action as RemoveHazardAction
		?? throw new ArgumentException($"Expected {nameof(RemoveHazardAction)}.", nameof(action));
}
