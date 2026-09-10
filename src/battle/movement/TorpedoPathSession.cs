using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Spatial;
using GrimSpace.Math.Grid;

namespace GrimSpace.Battle.Movement;

public sealed class TorpedoPathSession
{
	public required string ActorId { get; init; }
	public required Coord Origin { get; init; }
	public required BodyFrame Frame { get; init; }
	public List<Coord> Cells { get; } = [];
	public List<TorpedoMoveStepAction> Steps { get; } = [];
	public int PathApSpent { get; set; }
	public int MinPathApRemaining { get; set; }
	public int PathForwardSteps { get; set; }
	public int UsedDirectionsMask { get; set; }
	public bool SpinBraked { get; set; }
	public int MoveStartMomentumLevel { get; set; }
	public int MovementBuildupLevel { get; set; }
	public int MovementBuildupForwardSteps { get; set; }

	public MomentumConfig.Buildup MovementBuildup =>
		new(MovementBuildupLevel, MovementBuildupForwardSteps);
	public Coord EndPosition => Cells[^1];

	public static TorpedoPathSession Begin(
		string actorId,
		Coord origin,
		BodyFrame frame,
		int momentumLevel,
		int minPathApCost) =>
		new()
		{
			ActorId = actorId,
			Origin = origin,
			Frame = frame,
			MoveStartMomentumLevel = momentumLevel,
			MovementBuildupLevel = momentumLevel,
			MinPathApRemaining = minPathApCost,
		};

	public void ApplyStep(
		TorpedoMoveStepAction step,
		Coord destination,
		int stepApCost,
		int directionBit)
	{
		Steps.Add(step);
		Cells.Add(destination);
		UsedDirectionsMask |= directionBit;
		if (step.Direction == ESpatialOrientation.Forward)
			PathForwardSteps++;

		MinPathApRemaining = System.Math.Max(0, MinPathApRemaining - System.Math.Max(1, stepApCost));
		if (stepApCost > 0)
			PathApSpent += stepApCost;
	}

	public void MarkSpinBraked()
	{
		SpinBraked = true;
		MinPathApRemaining = 0;
	}

	public bool CanEnd(int minPathApCost)
	{
		if (Steps.Count == 0)
			return true;
		if (MinPathApRemaining != 0)
			return false;
		return SpinBraked || PathApSpent == 0 || PathApSpent >= minPathApCost;
	}

	public TorpedoPathSession Clone()
	{
		var clone = new TorpedoPathSession
		{
			ActorId = ActorId,
			Origin = Origin,
			Frame = Frame,
			PathApSpent = PathApSpent,
			MinPathApRemaining = MinPathApRemaining,
			PathForwardSteps = PathForwardSteps,
			UsedDirectionsMask = UsedDirectionsMask,
			SpinBraked = SpinBraked,
			MoveStartMomentumLevel = MoveStartMomentumLevel,
			MovementBuildupLevel = MovementBuildupLevel,
			MovementBuildupForwardSteps = MovementBuildupForwardSteps,
		};
		clone.Cells.AddRange(Cells);
		clone.Steps.AddRange(Steps);
		return clone;
	}
}
