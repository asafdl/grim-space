using GrimSpace.Battle.Board;
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
	int MomentumLoss) : IAction<BattleBoard, ActorSession>
{
	public IActionDef<IAction, BattleBoard, ActorSession, IEffect<BattleBoard, ActorSession>> Definition =>
		ResolveHazardDef.Instance;
}

public sealed class ResolveHazardDef
	: IActionDef<IAction, BattleBoard, ActorSession, IEffect<BattleBoard, ActorSession>>
{
	public static ResolveHazardDef Instance { get; } = new();

	public IEnumerable<IAction> Discover(BattleBoard world, ActorSession runtime, string actorId) => [];

	public ResolveHazardAction Bind(
		string actorId,
		EHazardKind kind,
		IEnumerable<Coord> cells,
		int damage,
		int momentumLoss) =>
		new(actorId, kind, cells.ToHashSet(), damage, momentumLoss);

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
		[new ResolveHazardEffect(action.Kind, action.Cells, action.Damage, action.MomentumLoss)];

	private static ResolveHazardAction Cast(IAction action) =>
		action as ResolveHazardAction ?? throw new ArgumentException($"Expected {nameof(ResolveHazardAction)}.", nameof(action));
}
