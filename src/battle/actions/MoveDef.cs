using GrimSpace.Battle.Board;
using GrimSpace.Battle.Effects;
using GrimSpace.Battle.Movement;
using GrimSpace.Battle.Movement.Enums;
using GrimSpace.Battle.Slices;
using GrimSpace.Battle.Spatial;
using GrimSpace.Core.Actions;
using GrimSpace.Math.Grid;

namespace GrimSpace.Battle.Actions;

public sealed class MoveDef : IActionDef
{
	public static MoveDef Instance { get; } = new();

	private static readonly EStepDirection[] AllDirections = Enum.GetValues<EStepDirection>();

	public IEnumerable<IAction> Discover(BattleActionContext ctx, string ownerId)
	{
		foreach (var direction in AllDirections)
		{
			var action = new MoveStepAction(ownerId, direction);
			if (IsPossible(action, ctx))
				yield return action;
		}
	}

	public bool IsPossible(IAction action, BattleActionContext ctx) =>
		IsPossible(Cast(action), ctx);

	public bool IsLegal(IAction action, BattleActionContext ctx) =>
		IsLegal(Cast(action), ctx);

	public IReadOnlyList<IEffect<BattleSlices>> Resolve(IAction action, BattleActionContext ctx) =>
		Resolve(Cast(action), ctx);

	public bool IsPossible(MoveStepAction action, BattleActionContext ctx)
	{
		var actor = ctx.Board.StateOf(action.OwnerId);
		var frame = BodyFrame.From(actor);
		var to = actor.Position + frame.Step(action.Direction);
		var blocked = ctx.Board.BlockedFor(action.OwnerId);
		return ctx.Board.Grid.IsInBounds(to) && !blocked.Contains(to);
	}

	public bool IsLegal(MoveStepAction action, BattleActionContext ctx)
	{
		if (!IsPossible(action, ctx))
			return false;

		var state = ctx.PhaseContext;
		if (MoveDirectionRules.UsesOpposite(state.UsedDirectionsMask, action.Direction))
			return false;

		var actor = ctx.Board.StateOf(action.OwnerId);
		var stepCost = StepCosts.GetMoveStepApCost(
			action.Direction,
			new MoveStepContext(state.PathForwardSteps, actor.MomentumLevel));

		return stepCost <= actor.ActionPoints;
	}

	public IReadOnlyList<IEffect<BattleSlices>> Resolve(MoveStepAction action, BattleActionContext ctx)
	{
		var board = ctx.Board;
		var state = ctx.PhaseContext;
		var actor = board.StateOf(action.OwnerId);
		var frame = BodyFrame.From(actor);
		var to = actor.Position + frame.Step(action.Direction);
		var directionBit = MoveDirectionRules.DirectionBit(action.Direction);
		var stepCost = StepCosts.GetMoveStepApCost(
			action.Direction,
			new MoveStepContext(state.PathForwardSteps, actor.MomentumLevel));

		var effects = new List<IEffect<BattleSlices>>();

		if (!state.IsMovePathStarted)
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

	private static MoveStepAction Cast(IAction action) =>
		action as MoveStepAction ?? throw new ArgumentException($"Expected {nameof(MoveStepAction)}.", nameof(action));
}
