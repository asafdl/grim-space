using GrimSpace.Battle.Board;
using GrimSpace.Battle.Effects;
using GrimSpace.Battle.Movement;
using GrimSpace.Battle.Movement.Enums;
using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.Spatial;
using GrimSpace.Core.Actions;
using GrimSpace.Math.Grid;

namespace GrimSpace.Battle.Actions;

public sealed class MoveDef
	: IActionDef<IAction, BattleBoard, ActorSession, IEffect<BattleBoard, ActorSession>>
{
	public static MoveDef Instance { get; } = new();

	private static readonly EStepDirection[] AllDirections = Enum.GetValues<EStepDirection>();

	public IEnumerable<IAction> Discover(BattleBoard world, ActorSession runtime, string ownerId)
	{
		foreach (var direction in AllDirections)
		{
			var action = new MoveStepAction(ownerId, direction);
			if (IsPossible(action, world, runtime))
				yield return action;
		}
	}

	public bool IsPossible(IAction action, BattleBoard world, ActorSession runtime) =>
		IsPossible(Cast(action), world, runtime);

	public bool IsLegal(IAction action, BattleBoard world, ActorSession runtime) =>
		IsLegal(Cast(action), world, runtime);

	public IReadOnlyList<IEffect<BattleBoard, ActorSession>> Resolve(
		IAction action,
		BattleBoard world,
		ActorSession runtime) =>
		Resolve(Cast(action), world, runtime);

	public bool IsPossible(MoveStepAction action, BattleBoard world, ActorSession runtime)
	{
		var actor = world.StateOf(action.OwnerId);
		var frame = BodyFrame.From(actor);
		var to = actor.Position + frame.Step(action.Direction);
		var blocked = world.BlockedFor(action.OwnerId);
		return world.Grid.IsInBounds(to) && !blocked.Contains(to);
	}

	public bool IsLegal(MoveStepAction action, BattleBoard world, ActorSession runtime)
	{
		if (!IsPossible(action, world, runtime))
			return false;

		if (MoveDirectionRules.UsesOpposite(runtime.UsedDirectionsMask, action.Direction))
			return false;

		var actor = world.StateOf(action.OwnerId);
		var stepCost = StepCosts.GetMoveStepApCost(
			action.Direction,
			new MoveStepContext(runtime.PathForwardSteps, actor.MomentumLevel));

		return stepCost <= actor.ActionPoints;
	}

	public IReadOnlyList<IEffect<BattleBoard, ActorSession>> Resolve(
		MoveStepAction action,
		BattleBoard world,
		ActorSession runtime)
	{
		var actor = world.StateOf(action.OwnerId);
		var frame = BodyFrame.From(actor);
		var to = actor.Position + frame.Step(action.Direction);
		var directionBit = MoveDirectionRules.DirectionBit(action.Direction);
		var stepCost = StepCosts.GetMoveStepApCost(
			action.Direction,
			new MoveStepContext(runtime.PathForwardSteps, actor.MomentumLevel));

		var effects = new List<IEffect<BattleBoard, ActorSession>>();

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
		string ownerId,
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

			steps.Add(new MoveStepAction(ownerId, direction));
			from = to;
		}

		return steps;
	}

	public IEnumerable<Option> DiscoverPaths(
		BattleBoard board,
		ActorSession runtime,
		string ownerId)
	{
		if (runtime.IsMovePathStarted)
			yield break;

		foreach (var option in MovePathFinder.Find(board, runtime, ownerId))
			yield return option;
	}

	private static MoveStepAction Cast(IAction action) =>
		action as MoveStepAction ?? throw new ArgumentException($"Expected {nameof(MoveStepAction)}.", nameof(action));
}
