using GrimSpace.Battle;
using GrimSpace.Battle.ActivationRound;
using GrimSpace.Battle.Effects;
using GrimSpace.Battle.Ids;
using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.World;
using GrimSpace.Core.Actions;

namespace GrimSpace.Battle.Actions;

public sealed record ProcessActivationRoundAction(
	string TargetUnitId,
	EUnitActivationPhase Phase) : IAction<BattleWorld, ActorRuntime>
{
	public string ActorId => BattleActorIds.Rules;

	public IActionDef<IAction, BattleWorld, ActorRuntime, IEffect<BattleWorld, ActorRuntime>> Definition =>
		ProcessActivationRoundDef.Instance;
}

public sealed class ProcessActivationRoundDef
	: IActionDef<IAction, BattleWorld, ActorRuntime, IEffect<BattleWorld, ActorRuntime>>
{
	public static ProcessActivationRoundDef Instance { get; } = new();

	public IEnumerable<IAction> Discover(BattleWorld world, ActorRuntime runtime, string actorId) => [];

	public ProcessActivationRoundAction Bind(string actorId) =>
		throw new NotSupportedException();

	public bool IsPossible(IAction action, BattleWorld world, ActorRuntime runtime) => true;

	public bool IsLegal(IAction action, BattleWorld world, ActorRuntime runtime) =>
		world.UnitRegistry.TryGet(Cast(action).TargetUnitId, out _);

	public IReadOnlyList<IEffect<BattleWorld, ActorRuntime>> Resolve(
		IAction action,
		BattleWorld world,
		ActorRuntime runtime) =>
		Resolve(Cast(action));

	public IReadOnlyList<IEffect<BattleWorld, ActorRuntime>> Resolve(ProcessActivationRoundAction action) =>
		[new EmitUnitActivationRoundPhaseEffect(action.TargetUnitId, action.Phase)];

	private static ProcessActivationRoundAction Cast(IAction action) =>
		action as ProcessActivationRoundAction
		?? throw new ArgumentException($"Expected {nameof(ProcessActivationRoundAction)}.", nameof(action));
}
