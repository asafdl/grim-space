using GrimSpace.Battle.World;
using GrimSpace.Battle.Effects;
using GrimSpace.Battle.Runtime;
using GrimSpace.Core.Actions;
using GrimSpace.Math.Grid;

namespace GrimSpace.Battle.Actions;

public sealed record ResolveHazardAction(
	string ActorId,
	EHazardKind Kind,
	HashSet<Coord> Cells,
	int Damage,
	int MomentumLoss) : IAction<BattleWorld, ActorRuntime>
{
	public IActionDef<IAction, BattleWorld, ActorRuntime, IEffect<BattleWorld, ActorRuntime>> Definition =>
		ResolveHazardDef.Instance;
}

public sealed class ResolveHazardDef
	: IActionDef<IAction, BattleWorld, ActorRuntime, IEffect<BattleWorld, ActorRuntime>>
{
	public static ResolveHazardDef Instance { get; } = new();

	public IEnumerable<IAction> Discover(BattleWorld world, ActorRuntime runtime, string actorId) => [];

	public ResolveHazardAction Bind(
		string actorId,
		EHazardKind kind,
		IEnumerable<Coord> cells,
		int damage,
		int momentumLoss) =>
		new(actorId, kind, cells.ToHashSet(), damage, momentumLoss);

	public bool IsPossible(IAction action, BattleWorld world, ActorRuntime runtime) => true;

	public bool IsLegal(IAction action, BattleWorld world, ActorRuntime runtime) => true;

	public IReadOnlyList<IEffect<BattleWorld, ActorRuntime>> Resolve(
		IAction action,
		BattleWorld world,
		ActorRuntime runtime) =>
		Resolve(Cast(action), world, runtime);

	public IReadOnlyList<IEffect<BattleWorld, ActorRuntime>> Resolve(
		ResolveHazardAction action,
		BattleWorld world,
		ActorRuntime runtime) =>
		[new ResolveHazardEffect(action.Kind, action.Cells, action.Damage, action.MomentumLoss)];

	private static ResolveHazardAction Cast(IAction action) =>
		action as ResolveHazardAction ?? throw new ArgumentException($"Expected {nameof(ResolveHazardAction)}.", nameof(action));
}
