using GrimSpace.Battle.Effects;
using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.Statuses;
using GrimSpace.Battle.World;
using GrimSpace.Core.Actions;
using GrimSpace.Pilots;

namespace GrimSpace.Battle.Actions;

public sealed record HeatVentsAction(string ActorId) : IAction<BattleWorld, ActorRuntime>
{
	public IActionDef<IAction, BattleWorld, ActorRuntime, IEffect<BattleWorld, ActorRuntime>> Definition =>
		HeatVentsDef.Instance;
}

public sealed class HeatVentsDef
	: IActionDef<IAction, BattleWorld, ActorRuntime, IEffect<BattleWorld, ActorRuntime>>,
		IActorActionDef
{
	public const int FollowUpDelayTicks = 1;

	public static HeatVentsDef Instance { get; } = new();

	public bool IsBlockable => false;

	public IEnumerable<IAction> Discover(BattleWorld world, ActorRuntime runtime, string actorId)
	{
		var action = Bind(actorId);
		if (IsPossible(action, world, runtime))
			yield return action;
	}

	public HeatVentsAction Bind(string actorId) => new(actorId);

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

	public bool IsPossible(HeatVentsAction action, BattleWorld world, ActorRuntime runtime) =>
		runtime.PilotCapabilities.Contains(EPilotCapabilityId.HeatVents);

	public bool IsLegal(HeatVentsAction action, BattleWorld world, ActorRuntime runtime)
	{
		if (!IsPossible(action, world, runtime))
			return false;

		var state = world.StateOf(action.ActorId);
		return !BattleStatuses.HasOverheat(state)
			&& !state.HeatVentsUsedThisTurn
			&& state.Heat > 0
			&& state.CurrentActionPoints >= PilotSkillPercents.HeatVents.ApCost;
	}

	public IReadOnlyList<IEffect<BattleWorld, ActorRuntime>> Resolve(
		HeatVentsAction action,
		BattleWorld world,
		ActorRuntime runtime)
	{
		var actorId = action.ActorId;
		return
		[
			new ApChangeEffect(-PilotSkillPercents.HeatVents.ApCost),
			new HeatChangeEffect(-PilotSkillPercents.HeatVents.HeatReleased),
			new SetHeatVentsUsedThisTurnEffect(true),
			new ScheduleActionEffect(
				FollowUpDelayTicks,
				new ResetHeatVentsUsedThisTurnAction(actorId)),
		];
	}

	private static HeatVentsAction Cast(IAction action) =>
		action is HeatVentsAction typed
			? typed
			: throw new ArgumentException($"Expected {nameof(HeatVentsAction)}.", nameof(action));
}
