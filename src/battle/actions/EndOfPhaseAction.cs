using GrimSpace.Battle.World;
using GrimSpace.Battle.Effects;
using GrimSpace.Battle.Runtime;
using GrimSpace.Core.Actions;

namespace GrimSpace.Battle.Actions;

public sealed record EndOfPhaseAction(string ActorId) : IAction<BattleWorld, ActorRuntime>
{
	public IActionDef<IAction, BattleWorld, ActorRuntime, IEffect<BattleWorld, ActorRuntime>> Definition =>
		EndOfPhaseDef.Instance;
}

public sealed class EndOfPhaseDef
	: IActionDef<IAction, BattleWorld, ActorRuntime, IEffect<BattleWorld, ActorRuntime>>
{
	public static EndOfPhaseDef Instance { get; } = new();

	public IEnumerable<IAction> Discover(BattleWorld world, ActorRuntime runtime, string actorId) => [];

	public EndOfPhaseAction Bind(string actorId) => new(actorId);

	public bool IsPossible(IAction action, BattleWorld world, ActorRuntime runtime) => true;

	public bool IsLegal(IAction action, BattleWorld world, ActorRuntime runtime) => true;

	public IReadOnlyList<IEffect<BattleWorld, ActorRuntime>> Resolve(
		IAction action,
		BattleWorld world,
		ActorRuntime runtime) =>
		Resolve(Cast(action), world, runtime);

	public IReadOnlyList<IEffect<BattleWorld, ActorRuntime>> Resolve(
		EndOfPhaseAction action,
		BattleWorld world,
		ActorRuntime runtime)
	{
		if (runtime.IsMovePathStarted)
			return [new EndMovePathEffect()];

		return [new MomentumDecayEffect()];
	}

	private static EndOfPhaseAction Cast(IAction action) =>
		action as EndOfPhaseAction ?? throw new ArgumentException($"Expected {nameof(EndOfPhaseAction)}.", nameof(action));
}
