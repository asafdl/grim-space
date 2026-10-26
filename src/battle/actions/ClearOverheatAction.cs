using GrimSpace.Battle.Effects;
using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.World;
using GrimSpace.Core.Actions;

namespace GrimSpace.Battle.Actions;

public sealed record ClearOverheatAction(string ActorId) : IAction<BattleWorld, ActorRuntime>
{
	public IActionDef<IAction, BattleWorld, ActorRuntime, IEffect<BattleWorld, ActorRuntime>> Definition =>
		ClearOverheatDef.Instance;
}

public sealed class ClearOverheatDef
	: IActionDef<IAction, BattleWorld, ActorRuntime, IEffect<BattleWorld, ActorRuntime>>
{
	public static ClearOverheatDef Instance { get; } = new();

	public IEnumerable<IAction> Discover(BattleWorld world, ActorRuntime runtime, string actorId) => [];

	public ClearOverheatAction Bind(string actorId) => new(actorId);

	public bool IsPossible(IAction action, BattleWorld world, ActorRuntime runtime) => true;

	public bool IsLegal(IAction action, BattleWorld world, ActorRuntime runtime) => true;

	public IReadOnlyList<IEffect<BattleWorld, ActorRuntime>> Resolve(
		IAction action,
		BattleWorld world,
		ActorRuntime runtime) =>
		action is ClearOverheatAction
			? [new ClearOverheatEffect()]
			: [];
}
