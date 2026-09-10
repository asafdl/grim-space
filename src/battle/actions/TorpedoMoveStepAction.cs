using GrimSpace.Battle.Effects;
using GrimSpace.Battle.Movement;
using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.Spatial;
using GrimSpace.Battle.World;
using GrimSpace.Core.Actions;
using GrimSpace.Math.Grid;
using GrimSpace.Units.Enums;

namespace GrimSpace.Battle.Actions;

public sealed record TorpedoMoveStepAction(
	string ActorId,
	ESpatialOrientation Direction) : IAction<BattleWorld, ActorRuntime>
{
	public IActionDef<IAction, BattleWorld, ActorRuntime, IEffect<BattleWorld, ActorRuntime>> Definition =>
		TorpedoMoveDef.Instance;
}

public sealed class TorpedoMoveDef
	: IActionDef<IAction, BattleWorld, ActorRuntime, IEffect<BattleWorld, ActorRuntime>>,
		IActionInvariants<BattleWorld, ActorRuntime>
{
	public static TorpedoMoveDef Instance { get; } = new();

	private static readonly ESpatialOrientation[] AllDirections = Enum.GetValues<ESpatialOrientation>();

	public IEnumerable<IAction> Discover(BattleWorld world, ActorRuntime runtime, string actorId)
	{
		foreach (var direction in AllDirections)
		{
			var action = Bind(actorId, direction);
			if (IsPossible(action, world, runtime))
				yield return action;
		}
	}

	public TorpedoMoveStepAction Bind(string actorId, ESpatialOrientation direction) =>
		new(actorId, direction);

	public bool IsPossible(IAction action, BattleWorld world, ActorRuntime runtime) =>
		IsPossible(Cast(action), world);

	public bool IsLegal(IAction action, BattleWorld world, ActorRuntime runtime) =>
		IsLegal(Cast(action), world, runtime);

	public IReadOnlyList<IEffect<BattleWorld, ActorRuntime>> Resolve(
		IAction action,
		BattleWorld world,
		ActorRuntime runtime) =>
		Resolve(Cast(action), world, runtime);

	public bool IsPossible(TorpedoMoveStepAction action, BattleWorld world)
	{
		var actor = world.StateOf(action.ActorId);
		if (actor.Type != EType.Torpedo)
			return false;

		var to = actor.Position + BodyFrame.From(actor).Step(action.Direction);
		return world.Grid.IsInBounds(to) && !world.BlockedFor(action.ActorId).Contains(to);
	}

	public bool IsLegal(TorpedoMoveStepAction action, BattleWorld world, ActorRuntime runtime)
	{
		if (!IsPossible(action, world))
			return false;

		var path = runtime.ActivePath;
		if (path is not null && DirectionRules.UsesOpposite(path.UsedDirectionsMask, action.Direction))
			return false;

		var actor = world.StateOf(action.ActorId);
		var stepCost = StepCosts.GetMoveStepApCost(
			action.Direction,
			new MoveStepContext(path?.PathForwardSteps ?? 0, actor.MomentumLevel));
		return stepCost <= actor.ActionPoints
			&& (stepCost != 0 || actor.ActionPoints != 0 || (path?.PathApSpent ?? 0) != 0);
	}

	public InvariantStatus EvaluateInvariants(BattleWorld world, ActorRuntime runtime, string actorId)
	{
		if (runtime.ActivePath is null
			|| runtime.ActivePath.CanEnd(world.StateOf(actorId).Stats.MinPathApCost))
			return InvariantStatus.Ok;

		return Discover(world, runtime, actorId).Any()
			? InvariantStatus.Incomplete
			: InvariantStatus.Impossible;
	}

	public IReadOnlyList<IEffect<BattleWorld, ActorRuntime>> Resolve(
		TorpedoMoveStepAction action,
		BattleWorld world,
		ActorRuntime runtime)
	{
		var actor = world.StateOf(action.ActorId);
		var to = actor.Position + BodyFrame.From(actor).Step(action.Direction);
		var path = runtime.ActivePath;
		var stepCost = StepCosts.GetMoveStepApCost(
			action.Direction,
			new MoveStepContext(path?.PathForwardSteps ?? 0, actor.MomentumLevel));

		return
		[
			new TorpedoPathStepEffect(action, to, stepCost, DirectionRules.DirectionBit(action.Direction)),
			new TorpedoStepMomentumEffect(action.Direction),
			new MoveEffect(to),
			new ApChangeEffect(-stepCost),
			new HazardCellEntryEffect(to),
		];
	}

	private static TorpedoMoveStepAction Cast(IAction action) =>
		action as TorpedoMoveStepAction
			?? throw new ArgumentException($"Expected {nameof(TorpedoMoveStepAction)}.", nameof(action));
}
