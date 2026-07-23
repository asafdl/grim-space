using GrimSpace.Battle.Board;
using GrimSpace.Battle.Effects;
using GrimSpace.Battle.Runtime;
using GrimSpace.Core.Actions;

namespace GrimSpace.Battle.Actions;

public sealed record EndOfPhaseAction(
	string ActorId,
	int? UndoGroup = null) : IAction<BattleBoard, ActorSession>
{
	public IActionDef<IAction, BattleBoard, ActorSession, IEffect<BattleBoard, ActorSession>> Definition =>
		EndOfPhaseDef.Instance;
}

public sealed class EndOfPhaseDef
	: IActionDef<IAction, BattleBoard, ActorSession, IEffect<BattleBoard, ActorSession>>
{
	public static EndOfPhaseDef Instance { get; } = new();

	public IEnumerable<IAction> Discover(BattleBoard world, ActorSession runtime, string actorId) => [];

	public EndOfPhaseAction Bind(string actorId) => new(actorId);

	public bool IsPossible(IAction action, BattleBoard world, ActorSession runtime) => true;

	public bool IsLegal(IAction action, BattleBoard world, ActorSession runtime) => true;

	public IReadOnlyList<IEffect<BattleBoard, ActorSession>> Resolve(
		IAction action,
		BattleBoard world,
		ActorSession runtime) =>
		Resolve(Cast(action), world, runtime);

	public IReadOnlyList<IEffect<BattleBoard, ActorSession>> Resolve(
		EndOfPhaseAction action,
		BattleBoard world,
		ActorSession runtime)
	{
		if (runtime.IsMovePathStarted)
			return [new EndMovePathEffect()];

		return [new MomentumDecayEffect()];
	}

	private static EndOfPhaseAction Cast(IAction action) =>
		action as EndOfPhaseAction ?? throw new ArgumentException($"Expected {nameof(EndOfPhaseAction)}.", nameof(action));
}
