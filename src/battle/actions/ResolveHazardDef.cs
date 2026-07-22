using GrimSpace.Battle.Board;
using GrimSpace.Battle.Effects;
using GrimSpace.Battle.Runtime;
using GrimSpace.Core.Actions;

namespace GrimSpace.Battle.Actions;

public sealed class ResolveHazardDef
	: IActionDef<IAction, BattleBoard, ActorSession, IEffect<BattleBoard, ActorSession>>
{
	public static ResolveHazardDef Instance { get; } = new();

	public IEnumerable<IAction> Discover(BattleBoard world, ActorSession runtime, string ownerId) => [];

	public bool IsPossible(IAction action, BattleBoard world, ActorSession runtime) => true;

	public bool IsLegal(IAction action, BattleBoard world, ActorSession runtime) => true;

	public IReadOnlyList<IEffect<BattleBoard, ActorSession>> Resolve(
		IAction action,
		BattleBoard world,
		ActorSession runtime) =>
		Resolve(Cast(action), world, runtime);

	public IReadOnlyList<IEffect<BattleBoard, ActorSession>> Resolve(
		ResolveHazardAction action,
		BattleBoard world,
		ActorSession runtime) =>
		[new ResolveHazardEffect(action.HazardId), new RemoveHazardEffect(action.HazardId)];

	private static ResolveHazardAction Cast(IAction action) =>
		action as ResolveHazardAction ?? throw new ArgumentException($"Expected {nameof(ResolveHazardAction)}.", nameof(action));
}
