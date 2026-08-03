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
	public const int InitialMinPathApCost = 3;

	public required string ActorId { get; init; }
	public required Coord Origin { get; init; }
	public required BodyFrame Frame { get; init; }
	public List<Coord> Cells { get; } = [];
	public List<MoveStepAction> Steps { get; } = [];
	public int PathApSpent { get; set; }
	public int MinPathApCost { get; set; } = InitialMinPathApCost;
	public int PathForwardSteps { get; set; }
	public int UsedDirectionsMask { get; set; }
	public bool SpinBraked { get; set; }
	public int MoveStartMomentumLevel { get; set; }
	public int MovementBuildupLevel { get; set; }
	public int MovementBuildupForwardSteps { get; set; }

	public Coord EndPosition => Cells[^1];

	public MomentumConfig.Buildup MovementBuildup =>
		new(MovementBuildupLevel, MovementBuildupForwardSteps);

	public int ExtensionApCost(int baselinePathApSpent) => PathApSpent - baselinePathApSpent;

	public static MovePathSession Begin(string actorId, Coord origin, BodyFrame frame, int momentumLevel) =>
		new()
		{
			ActorId = actorId,
			Origin = origin,
			Frame = frame,
			MoveStartMomentumLevel = momentumLevel,
			MovementBuildupLevel = momentumLevel,
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
		MinPathApCost = System.Math.Max(0, MinPathApCost - minPathConsumption);
		if (stepApCost > 0)
			PathApSpent += stepApCost;
	}

	public void MarkSpinBraked()
	{
		SpinBraked = true;
		MinPathApCost = 0;
	}

	public bool CanEnd()
	{
		if (Steps.Count == 0)
			return true;

		if (MinPathApCost != 0)
			return false;

		if (SpinBraked)
			return true;

		return PathApSpent == 0 || PathApSpent >= InitialMinPathApCost;
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
			MinPathApCost = MinPathApCost,
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
