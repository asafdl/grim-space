using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.Spatial;
using GrimSpace.Math.Grid;

namespace GrimSpace.Battle.Movement;

/// <summary>
/// Live or cached move-path state: geometry, steps, and path-scoped budget/buildup.
/// </summary>
public sealed class MovePathSession
{
	public required string ActorId { get; init; }
	public required Coord Origin { get; init; }
	public required BodyFrame Frame { get; init; }
	public List<Coord> Cells { get; } = [];
	public List<MoveStepAction> Steps { get; } = [];
	public int PathApSpent { get; set; }
	public int MinPathApRemaining { get; set; }
	public int PathForwardSteps { get; set; }
	public int UsedDirectionsMask { get; set; }
	public bool SpinBraked { get; set; }
	public int MoveStartMomentumLevel { get; set; }
	public int MovementBuildupLevel { get; set; }
	public int MovementBuildupForwardSteps { get; set; }

	public bool CanEndPath { get; set; } = true;

	public Coord EndPosition => Cells[^1];

	public MomentumConfig.Buildup MovementBuildup =>
		new(MovementBuildupLevel, MovementBuildupForwardSteps);

	public int ExtensionApCost(int baselinePathApSpent) => PathApSpent - baselinePathApSpent;

	public static MovePathSession Begin(
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
		MoveStepAction step,
		Coord destination,
		int stepApCost,
		int directionBit)
	{
		Steps.Add(step);
		Cells.Add(destination);

		UsedDirectionsMask |= directionBit;
		if (step.Direction == ESpatialOrientation.Forward)
			PathForwardSteps++;

		var minPathConsumption = System.Math.Max(1, stepApCost);
		MinPathApRemaining = System.Math.Max(0, MinPathApRemaining - minPathConsumption);
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

		if (SpinBraked)
			return true;

		return PathApSpent == 0 || PathApSpent >= minPathApCost;
	}

	public static bool PreferPath(MovePathSession candidate, MovePathSession existing) =>
		candidate.Steps.Count < existing.Steps.Count
		|| candidate.Steps.Count == existing.Steps.Count && candidate.PathApSpent < existing.PathApSpent;

	public MovePathSession Clone()
	{
		var clone = new MovePathSession
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
			CanEndPath = CanEndPath,
		};
		clone.Cells.AddRange(Cells);
		clone.Steps.AddRange(Steps);
		return clone;
	}
}
