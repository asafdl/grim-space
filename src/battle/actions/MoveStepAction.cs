using GrimSpace.Battle.World;
using GrimSpace.Battle.Effects;
using GrimSpace.Battle.Movement;
using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.Spatial;
using GrimSpace.Core.Actions;
using GrimSpace.Math.Grid;

namespace GrimSpace.Battle.Actions;

public sealed record MoveStepAction(
	string ActorId,
	ESpatialOrientation Direction) : IAction<BattleWorld, ActorRuntime>
{
	public IActionDef<IAction, BattleWorld, ActorRuntime, IEffect<BattleWorld, ActorRuntime>> Definition =>
		MoveDef.Instance;
}

public sealed class MoveDef
	: IActionDef<IAction, BattleWorld, ActorRuntime, IEffect<BattleWorld, ActorRuntime>>,
		IActionInvariants<BattleWorld, ActorRuntime>
{
	public static MoveDef Instance { get; } = new();

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

	public MoveStepAction Bind(string actorId, ESpatialOrientation direction) =>
		new(actorId, direction);

	public bool IsPossible(IAction action, BattleWorld world, ActorRuntime runtime) =>
		IsPossible(Cast(action), world, runtime);

	public bool IsLegal(IAction action, BattleWorld world, ActorRuntime runtime) =>
		IsLegal(Cast(action), world, runtime);

	public IReadOnlyList<IEffect<BattleWorld, ActorRuntime>> Resolve(
		IAction action,
		BattleWorld world,
		ActorRuntime runtime) =>
		Resolve(Cast(action), world, runtime);

	public bool IsPossible(MoveStepAction action, BattleWorld world, ActorRuntime runtime)
	{
		var actor = world.StateOf(action.ActorId);
		var frame = BodyFrame.From(actor);
		var to = actor.Position + frame.Step(action.Direction);
		var blocked = world.BlockedFor(action.ActorId);
		return world.Grid.IsInBounds(to) && !blocked.Contains(to);
	}

	public bool IsLegal(MoveStepAction action, BattleWorld world, ActorRuntime runtime)
	{
		if (!IsPossible(action, world, runtime))
			return false;

		var path = runtime.ActivePath;
		if (path is not null && DirectionRules.UsesOpposite(path.UsedDirectionsMask, action.Direction))
			return false;

		var actor = world.StateOf(action.ActorId);
		var forwardSteps = path?.PathForwardSteps ?? 0;
		var pathApSpent = path?.PathApSpent ?? 0;
		var stepCost = StepCosts.GetMoveStepApCost(
			action.Direction,
			new MoveStepContext(forwardSteps, actor.MomentumLevel));

		if (stepCost > actor.ActionPoints)
			return false;

		if (stepCost == 0 && actor.ActionPoints == 0 && pathApSpent == 0)
			return false;

		return true;
	}

	public InvariantStatus EvaluateInvariants(BattleWorld world, ActorRuntime runtime, string actorId)
	{
		if (runtime.ActivePath is null)
			return InvariantStatus.Ok;

		if (runtime.ActivePath.CanEnd())
			return InvariantStatus.Ok;

		foreach (var candidate in Discover(world, runtime, actorId))
		{
			if (IsPossible(candidate, world, runtime))
				return InvariantStatus.Incomplete;
		}

		return InvariantStatus.Impossible;
	}

	public IReadOnlyList<IEffect<BattleWorld, ActorRuntime>> Resolve(
		MoveStepAction action,
		BattleWorld world,
		ActorRuntime runtime)
	{
		var actor = world.StateOf(action.ActorId);
		var frame = BodyFrame.From(actor);
		var to = actor.Position + frame.Step(action.Direction);
		var directionBit = DirectionRules.DirectionBit(action.Direction);
		var path = runtime.ActivePath;
		var forwardSteps = path?.PathForwardSteps ?? 0;
		var stepCost = StepCosts.GetMoveStepApCost(
			action.Direction,
			new MoveStepContext(forwardSteps, actor.MomentumLevel));

		return [
			new MovePathStepEffect(action, to, stepCost, directionBit),
			new MoveStepMomentumEffect(action.Direction),
			new MoveEffect(to),
			new ApChangeEffect(-stepCost),
			new HazardCellEntryEffect(to),
		];
	}

	private static MoveStepAction Cast(IAction action) =>
		action as MoveStepAction ?? throw new ArgumentException($"Expected {nameof(MoveStepAction)}.", nameof(action));
}
