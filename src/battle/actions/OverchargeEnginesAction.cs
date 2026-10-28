using GrimSpace.Battle.Effects;
using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.Units;
using GrimSpace.Battle.World;
using GrimSpace.Core.Actions;
using GrimSpace.Pilots;

namespace GrimSpace.Battle.Actions;

public sealed record OverchargeEnginesAction(string ActorId) : IAction<BattleWorld, ActorRuntime>
{
	public IActionDef<IAction, BattleWorld, ActorRuntime, IEffect<BattleWorld, ActorRuntime>> Definition =>
		OverchargeEnginesDef.Instance;
}

public sealed class OverchargeEnginesDef
	: IActionDef<IAction, BattleWorld, ActorRuntime, IEffect<BattleWorld, ActorRuntime>>,
		IActorActionDef
{
	public const int FollowUpDelayTicks = 1;

	public static OverchargeEnginesDef Instance { get; } = new();

	public bool IsBlockable => false;

	public IEnumerable<IAction> Discover(BattleWorld world, ActorRuntime runtime, string actorId)
	{
		var action = Bind(actorId);
		if (IsPossible(action, world, runtime))
			yield return action;
	}

	public OverchargeEnginesAction Bind(string actorId) => new(actorId);

	IAction IActorActionDef.Bind(string actorId) => Bind(actorId);

	public bool IsPossible(IAction action, BattleWorld world, ActorRuntime runtime) =>
		IsPossible(Cast(action), world, runtime);

	public bool IsLegal(IAction action, BattleWorld world, ActorRuntime runtime) =>
		IsLegal(Cast(action), world, runtime);

	public IReadOnlyList<IEffect<BattleWorld, ActorRuntime>> Resolve(
		IAction action,
		BattleWorld world,
		ActorRuntime runtime) =>
		Resolve(Cast(action), world, runtime);

	public bool IsPossible(OverchargeEnginesAction action, BattleWorld world, ActorRuntime runtime) =>
		runtime.PilotCapabilities.Contains(EPilotCapabilityId.OverchargeEngines);

	public bool IsLegal(OverchargeEnginesAction action, BattleWorld world, ActorRuntime runtime)
	{
		if (!IsPossible(action, world, runtime))
			return false;

		var state = world.StateOf(action.ActorId);
		return !state.Overheat
			&& state.OverchargeEnginesCooldownRemaining == 0
			&& state.ActionPoints >= PilotSkillPercents.OverchargeEngines.MinimumApToUse;
	}

	public IReadOnlyList<IEffect<BattleWorld, ActorRuntime>> Resolve(
		OverchargeEnginesAction action,
		BattleWorld world,
		ActorRuntime runtime)
	{
		var actorId = action.ActorId;
		var nextTurnPenalty = PilotSkillPercents.OverchargeEngines.NextTurnApPenalty;
		return
		[
			new RefreshActionPointsEffect(PilotSkillPercents.OverchargeEngines.RefreshedAp),
			new SetOverchargeEnginesCooldownEffect(PilotSkillPercents.OverchargeEngines.CooldownTurns),
			new ScheduleActionEffect(FollowUpDelayTicks, new ApDeltaAction(actorId, -nextTurnPenalty)),
		];
	}

	private static OverchargeEnginesAction Cast(IAction action) =>
		action is OverchargeEnginesAction typed
			? typed
			: throw new ArgumentException($"Expected {nameof(OverchargeEnginesAction)}.", nameof(action));
}
