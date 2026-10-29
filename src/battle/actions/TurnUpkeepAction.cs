using GrimSpace.Battle.World;
using GrimSpace.Battle.Effects;
using GrimSpace.Battle.Runtime;
using GrimSpace.Core.Actions;

namespace GrimSpace.Battle.Actions;

public sealed record TurnUpkeepAction(string ActorId) : IAction<BattleWorld, ActorRuntime>
{
	public IActionDef<IAction, BattleWorld, ActorRuntime, IEffect<BattleWorld, ActorRuntime>> Definition =>
		TurnUpkeepDef.Instance;
}

public sealed class TurnUpkeepDef
	: IActionDef<IAction, BattleWorld, ActorRuntime, IEffect<BattleWorld, ActorRuntime>>
{
	public static TurnUpkeepDef Instance { get; } = new();

	public IEnumerable<IAction> Discover(BattleWorld world, ActorRuntime runtime, string actorId) => [];

	public TurnUpkeepAction Bind(string actorId) => new(actorId);

	public bool IsPossible(IAction action, BattleWorld world, ActorRuntime runtime) => true;

	public bool IsLegal(IAction action, BattleWorld world, ActorRuntime runtime) => true;

	public IReadOnlyList<IEffect<BattleWorld, ActorRuntime>> Resolve(
		IAction action,
		BattleWorld world,
		ActorRuntime runtime) =>
		[new TurnUpkeepEffect()];
}
