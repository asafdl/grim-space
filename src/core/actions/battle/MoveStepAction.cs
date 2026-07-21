using GrimSpace.Battle.Movement;
using GrimSpace.Battle.Movement.Enums;
using GrimSpace.Battle.Spatial;
using GrimSpace.Battle.Units;
using GrimSpace.Core.Actions;
using GrimSpace.Core.Actions.Battle.Contexts;
using GrimSpace.Core.Actions.Battle.Effects;
using GrimSpace.Math.Grid;

namespace GrimSpace.Core.Actions.Battle;

public sealed class MoveStepAction(
	string ownerId,
	Coord from,
	Coord to,
	int usedDirectionsMaskBefore,
	int? undoGroup = null) : IAction
{
	public string OwnerId { get; } = ownerId;
	public int? UndoGroup { get; } = undoGroup;
	public Coord From { get; } = from;
	public Coord To { get; } = to;
	public int UsedDirectionsMaskBefore { get; } = usedDirectionsMaskBefore;

	public bool IsLegal(BattleBoard board, BattlePlanContext context)
	{
		var actor = board.StateOf(OwnerId);
		if (actor.Position != From)
			return false;

		var blocked = board.BlockedFor(OwnerId);
		if (!board.Grid.IsInBounds(To) || blocked.Contains(To))
			return false;

		var frame = BodyFrame.From(actor);
		if (frame.DirectionOfStep(From, To) is not EStepDirection direction)
			return false;

		if (MoveDirectionRules.UsesOpposite(UsedDirectionsMaskBefore, direction))
			return false;

		var stepCost = StepCosts.GetMoveStepApCost(
			direction,
			new MoveStepContext(context.TurnState.PathForwardSteps, actor.MomentumLevel));

		return stepCost <= actor.ActionPoints;
	}

	public IReadOnlyList<IEffect<BattleSlices>> Resolve(BattleBoard board, BattlePlanContext context)
	{
		var actor = board.StateOf(OwnerId);
		var frame = BodyFrame.From(actor);
		var direction = frame.DirectionOfStep(From, To)
			?? throw new InvalidOperationException("Move step direction is undefined.");
		var directionBit = MoveDirectionRules.DirectionBit(direction);
		var stepCost = StepCosts.GetMoveStepApCost(
			direction,
			new MoveStepContext(context.TurnState.PathForwardSteps, actor.MomentumLevel));

		var effects = new List<IEffect<BattleSlices>>();

		if (!context.TurnState.IsMovePathStarted)
			effects.Add(new BeginMovePathEffect());

		effects.AddRange(
		[
			new MoveStepMomentumEffect(direction),
			new MoveEffect(To),
			new ApChangeEffect(-stepCost),
			new ConsumeMinPathApEffect(stepCost),
			new RecordMovePathStepEffect(direction, directionBit),
		]);

		if (direction == EStepDirection.Retro)
			effects.Add(new MarkSpinBrakedEffect());

		effects.Add(new HazardCellEntryEffect(To));

		return effects;
	}

	public static IReadOnlyList<MoveStepAction> BuildSteps(
		string ownerId,
		BodyFrame frame,
		Coord origin,
		IReadOnlyList<Coord> path)
	{
		var steps = new List<MoveStepAction>();
		var from = origin;
		var usedMask = 0;

		foreach (var to in path)
		{
			steps.Add(new MoveStepAction(ownerId, from, to, usedMask));
			if (frame.DirectionOfStep(from, to) is EStepDirection direction)
				usedMask |= MoveDirectionRules.DirectionBit(direction);

			from = to;
		}

		return steps;
	}
}
