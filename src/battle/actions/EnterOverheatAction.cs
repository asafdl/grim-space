using GrimSpace.Battle.Effects;
using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.World;
using GrimSpace.Core.Actions;

namespace GrimSpace.Battle.Actions;

public sealed record EnterOverheatAction(string ActorId) : IAction<BattleWorld, ActorRuntime>
{
	public IActionDef<IAction, BattleWorld, ActorRuntime, IEffect<BattleWorld, ActorRuntime>> Definition =>
		EnterOverheatDef.Instance;
}

public sealed class EnterOverheatDef
	: IActionDef<IAction, BattleWorld, ActorRuntime, IEffect<BattleWorld, ActorRuntime>>
{
	public const int ClearDelayTicks = 1;

	public static EnterOverheatDef Instance { get; } = new();

	public IEnumerable<IAction> Discover(BattleWorld world, ActorRuntime runtime, string actorId) => [];

	public EnterOverheatAction Bind(string actorId) => new(actorId);

	public bool IsPossible(IAction action, BattleWorld world, ActorRuntime runtime) => true;

	public bool IsLegal(IAction action, BattleWorld world, ActorRuntime runtime) => true;

	public IReadOnlyList<IEffect<BattleWorld, ActorRuntime>> Resolve(
		IAction action,
		BattleWorld world,
		ActorRuntime runtime) =>
		action is EnterOverheatAction typed
			?
			[
				new EnterOverheatEffect(),
				new ScheduleActionEffect(ClearDelayTicks, new ClearOverheatAction(typed.ActorId)),
			]
			: [];
}
