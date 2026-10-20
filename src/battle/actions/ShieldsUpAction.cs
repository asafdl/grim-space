using GrimSpace.Battle.Effects;
using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.Units;
using GrimSpace.Battle.World;
using GrimSpace.Core.Actions;
using GrimSpace.Pilots;

namespace GrimSpace.Battle.Actions;

public sealed record ShieldsUpAction(string ActorId) : IAction<BattleWorld, ActorRuntime>
{
	public IActionDef<IAction, BattleWorld, ActorRuntime, IEffect<BattleWorld, ActorRuntime>> Definition =>
		ShieldsUpDef.Instance;
}

public sealed class ShieldsUpDef
	: IActionDef<IAction, BattleWorld, ActorRuntime, IEffect<BattleWorld, ActorRuntime>>,
		IActorActionDef
{
	public const int NextTickApPenalty = 2;
	public const int TemporaryShieldPerFace = 1;
	public const int FollowUpDelayTicks = 1;

	public static ShieldsUpDef Instance { get; } = new();

	public bool IsBlockable => false;

	public IEnumerable<IAction> Discover(BattleWorld world, ActorRuntime runtime, string actorId)
	{
		var action = Bind(actorId);
		if (IsPossible(action, world, runtime))
			yield return action;
	}

	public ShieldsUpAction Bind(string actorId) => new(actorId);

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

	public bool IsPossible(ShieldsUpAction action, BattleWorld world, ActorRuntime runtime) =>
		runtime.PilotCapabilities.Contains(EPilotCapabilityId.ShieldBoost);

	public bool IsLegal(ShieldsUpAction action, BattleWorld world, ActorRuntime runtime)
	{
		if (!IsPossible(action, world, runtime))
			return false;

		return !world.StateOf(action.ActorId).ShieldsUpUsedThisTurn;
	}

	public IReadOnlyList<IEffect<BattleWorld, ActorRuntime>> Resolve(
		ShieldsUpAction action,
		BattleWorld world,
		ActorRuntime runtime)
	{
		var grantId = TemporaryShields.NewGrantId();
		var actorId = action.ActorId;
		var delay = FollowUpDelayTicks;
		return
		[
			new GrantTemporaryShieldPerFaceEffect(grantId, TemporaryShieldPerFace),
			new ShieldsUpActivateEffect(),
			new ScheduleActionEffect(delay, new ExpireTemporaryShieldGrantAction(actorId, grantId)),
			new ScheduleActionEffect(delay, new ApDeltaAction(actorId, -NextTickApPenalty)),
			new ScheduleActionEffect(delay, new ResetShieldsUpUsedThisTurnAction(actorId)),
		];
	}

	private static ShieldsUpAction Cast(IAction action) =>
		action is ShieldsUpAction typed
			? typed
			: throw new ArgumentException($"Expected {nameof(ShieldsUpAction)}.", nameof(action));
}
