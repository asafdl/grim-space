using GrimSpace.Battle;
using GrimSpace.Battle.Effects;
using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.Units;
using GrimSpace.Battle.World;
using GrimSpace.Core.Actions;
using GrimSpace.Math.Grid;
using GrimSpace.Pilots;

namespace GrimSpace.Battle.Actions;

public sealed record BraceAction(string ActorId, ESpatialOrientation Face)
	: IAction<BattleWorld, ActorRuntime>, IMountedAction
{
	public ESpatialOrientation MountedOn => Face;

	public IActionDef<IAction, BattleWorld, ActorRuntime, IEffect<BattleWorld, ActorRuntime>> Definition =>
		BraceDef.Instance;
}

public sealed class BraceDef
	: IActionDef<IAction, BattleWorld, ActorRuntime, IEffect<BattleWorld, ActorRuntime>>,
		IMountedActionDef
{
	public const int FollowUpDelayTicks = 1;
	public const int ExpiryEndPasses = 2;

	public static BraceDef Instance { get; } = new();

	public bool IsBlockable => false;

	public IEnumerable<IAction> Discover(BattleWorld world, ActorRuntime runtime, string actorId)
	{
		if (!runtime.PilotCapabilities.Contains(EPilotCapabilityId.ShieldBrace))
			yield break;

		foreach (var face in Enum.GetValues<ESpatialOrientation>())
		{
			var action = Bind(actorId, face);
			if (IsLegal(action, world, runtime))
				yield return action;
		}
	}

	public BraceAction Bind(string actorId, ESpatialOrientation face) => new(actorId, face);

	IAction IMountedActionDef.Bind(string actorId, ESpatialOrientation mountedOn) =>
		Bind(actorId, mountedOn);

	public bool IsPossible(IAction action, BattleWorld world, ActorRuntime runtime) =>
		IsPossible(Cast(action), world, runtime);

	public bool IsLegal(IAction action, BattleWorld world, ActorRuntime runtime) =>
		IsLegal(Cast(action), world, runtime);

	public IReadOnlyList<IEffect<BattleWorld, ActorRuntime>> Resolve(
		IAction action,
		BattleWorld world,
		ActorRuntime runtime) =>
		Resolve(Cast(action), world, runtime);

	public bool IsPossible(BraceAction action, BattleWorld world, ActorRuntime runtime) =>
		runtime.PilotCapabilities.Contains(EPilotCapabilityId.ShieldBrace);

	public bool IsLegal(BraceAction action, BattleWorld world, ActorRuntime runtime)
	{
		if (!IsPossible(action, world, runtime))
			return false;

		var state = world.StateOf(action.ActorId);
		return !state.Overheat
			&& !state.ShieldBraceActive
			&& !state.ShieldBraceUsedThisTurn
			&& ShieldBrace.TotalPermanentShields(state) > 0;
	}

	public IReadOnlyList<IEffect<BattleWorld, ActorRuntime>> Resolve(
		BraceAction action,
		BattleWorld world,
		ActorRuntime runtime)
	{
		var actorId = action.ActorId;
		var delay = FollowUpDelayTicks;
		return
		[
			new HeatChangeEffect(HeatChangeEffect.PerAbilityUse),
			new ActivateShieldBraceEffect(action.Face),
			new ScheduleActivationRoundEffect(
				EUnitActivationPhase.End,
				ExpiryEndPasses,
				sticky: false,
				new ReleaseShieldBraceAction(actorId)),
			new ScheduleActionEffect(delay, new ResetShieldBraceUsedThisTurnAction(actorId)),
		];
	}

	private static BraceAction Cast(IAction action) =>
		action is BraceAction typed
			? typed
			: throw new ArgumentException($"Expected {nameof(BraceAction)}.", nameof(action));
}
