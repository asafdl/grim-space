using GrimSpace.Battle.Movement;
using GrimSpace.Math.Grid;

namespace GrimSpace.Tests.Movement;

/// <summary>
/// Documented movement rules — tests assert against these expectations.
/// </summary>
internal static class MovementExpectations
{
	public const int FighterApPerTurn = 4;
	public const int MinApSpentOnMove = 3;
	public const int MaxMomentum = 3;
	public const int ForwardCostAfterFree = 1;

	/// <summary>First N forward steps in a path are free; N equals current momentum.</summary>
	public static int FreeForwardStepsAt(int momentum) => momentum;

	public static int ForwardStepApCost(int forwardStepsInPath, int momentumAtStep) =>
		forwardStepsInPath < FreeForwardStepsAt(momentumAtStep)
			? 0
			: ForwardCostAfterFree;

	public static int TotalApForPureForwardPath(int startMomentum, int stepCount)
	{
		var momentum = startMomentum;
		var forwardSteps = 0;
		var totalAp = 0;

		for (var step = 0; step < stepCount; step++)
		{
			totalAp += ForwardStepApCost(forwardSteps, momentum);

			forwardSteps++;
			momentum = System.Math.Min(momentum + 1, MaxMomentum);
		}

		return totalAp;
	}

	public static int MomentumAfterPureForwardPath(int startMomentum, int stepCount) =>
		System.Math.Min(startMomentum + stepCount, MaxMomentum);

	/// <summary>Valid move endpoints spend at least 3 AP, unless the whole path is free.</summary>
	public static bool IsValidMoveEndpoint(int totalApSpent) =>
		totalApSpent == 0 || totalApSpent >= MinApSpentOnMove;

	public static Option PureForwardMove(Coord origin, int stepCount, int startMomentum)
	{
		var path = new Coord[stepCount];
		for (var i = 0; i < stepCount; i++)
			path[i] = origin + Coord.Forward * (i + 1);

		return new Option
		{
			Path = path,
			ApCost = TotalApForPureForwardPath(startMomentum, stepCount),
		};
	}
}
