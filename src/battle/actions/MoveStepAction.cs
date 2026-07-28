using GrimSpace.Battle.World;
using GrimSpace.Battle.Effects;
using GrimSpace.Battle.Movement;
using GrimSpace.Battle.Movement.Enums;
using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.Spatial;
using GrimSpace.Core.Actions;
using GrimSpace.Math.Grid;

namespace GrimSpace.Battle.Actions;

public sealed record MoveStepAction(
	string ActorId,
	EStepDirection Direction) : IAction<BattleWorld, ActorRuntime>
{
	public IActionDef<IAction, BattleWorld, ActorRuntime, IEffect<BattleWorld, ActorRuntime>> Definition =>
		MoveDef.Instance;
}

public sealed class MoveDef
	: IActionDef<IAction, BattleWorld, ActorRuntime, IEffect<BattleWorld, ActorRuntime>>,
		IActionInvariants<BattleWorld, ActorRuntime>
{
	public static MoveDef Instance { get; } = new();

	private static readonly EStepDirection[] AllDirections = Enum.GetValues<EStepDirection>();

	public IEnumerable<IAction> Discover(BattleWorld world, ActorRuntime runtime, string actorId)
	{
		foreach (var direction in AllDirections)
		{
			var action = Bind(actorId, direction);
			if (IsPossible(action, world, runtime))
				yield return action;
		}
	}

	public MoveStepAction Bind(string actorId, EStepDirection direction) =>
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

		if (MoveDirectionRules.UsesOpposite(runtime.UsedDirectionsMask, action.Direction))
			return false;

		var actor = world.StateOf(action.ActorId);
		var stepCost = StepCosts.GetMoveStepApCost(
			action.Direction,
			new MoveStepContext(runtime.PathForwardSteps, actor.MomentumLevel));

		if (stepCost > actor.ActionPoints)
			return false;

		if (stepCost == 0 && actor.ActionPoints == 0 && runtime.PathApSpent == 0)
			return false;

		return true;
	}

	public InvariantStatus EvaluateInvariants(BattleWorld world, ActorRuntime runtime, string actorId)
	{
		if (!runtime.IsMovePathStarted)
			return InvariantStatus.Ok;

		if (MovePathRules.CanEndMovePath(runtime))
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
		var directionBit = MoveDirectionRules.DirectionBit(action.Direction);
		var stepCost = StepCosts.GetMoveStepApCost(
			action.Direction,
			new MoveStepContext(runtime.PathForwardSteps, actor.MomentumLevel));

		var effects = new List<IEffect<BattleWorld, ActorRuntime>>();

		if (!runtime.IsMovePathStarted)
			effects.Add(new BeginMovePathEffect());

		effects.AddRange(
		[
			new MoveStepMomentumEffect(action.Direction),
			new MoveEffect(to),
			new ApChangeEffect(-stepCost),
			new ConsumeMinPathApEffect(stepCost),
			new RecordMovePathStepEffect(action.Direction, directionBit),
		]);

		if (action.Direction == EStepDirection.Retro)
			effects.Add(new MarkSpinBrakedEffect());

		effects.Add(new HazardCellEntryEffect(to));

		return effects;
	}

	public static IReadOnlyList<MoveStepAction> StepsFromPath(
		string actorId,
		BodyFrame frame,
		Coord origin,
		IReadOnlyList<Coord> path)
	{
		var steps = new List<MoveStepAction>();
		var from = origin;

		foreach (var to in path)
		{
			if (frame.DirectionOfStep(from, to) is not EStepDirection direction)
				throw new InvalidOperationException("Move step direction is undefined.");

			steps.Add(Instance.Bind(actorId, direction));
			from = to;
		}

		return steps;
	}

	private static MoveStepAction Cast(IAction action) =>
		action as MoveStepAction ?? throw new ArgumentException($"Expected {nameof(MoveStepAction)}.", nameof(action));
}
