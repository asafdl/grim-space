using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Movement;
using GrimSpace.Math.Grid;

namespace GrimSpace.Tests.Movement;

/// <summary>
/// Documented movement rules — tests assert against these expectations.
/// </summary>
internal static class MovementExpectations
{
	public const int FighterApPerTurn = 4;
	public const int MaxMomentum = 3;
	public const int ForwardCostAfterFree = 1;

	/// <summary>
	/// At momentum 0 every step costs 1 AP, so 4 AP reaches the 3D Manhattan ball of radius 4
	/// (excluding origin): Σ_{d=1..4} (4d² + 2) = 6 + 18 + 38 + 66 = 128.
	/// </summary>
	public const int ReachableCellsAtMomentum0With4Ap = 128;

	/// <summary>
	/// Momentum 1 / 4 AP piecewise reachability in body frame (no opposite directions):
	/// L=0: 9; L=1: 28; L=2: 32; L=3: 12 → 81.
	/// </summary>
	public const int ReachableCellsAtMomentum1With4Ap = 81;

	/// <summary>
	/// Momentum 2 / 4 AP (unbounded): L=0 → 10 (F1..7, R1..3); L=1 → 28 (F0..4×4, R1..2×4) → 38.
	/// </summary>
	public const int ReachableCellsAtMomentum2With4Ap = 38;

	/// <summary>
	/// Momentum 3 / 4 AP (unbounded): L=0 → 9 (F1..7, R1..2); L=1 → 16 (F0..3×4); no retro+lateral → 25.
	/// </summary>
	public const int ReachableCellsAtMomentum3With4Ap = 25;

	/// <summary>4 AP preview expectations on an unbounded (or spacious) grid.</summary>
	public static TheoryData<int, int, int, int> ReachablePreviewByMomentum { get; } = new()
	{
		// momentum, visible cells, CanEndPath cells, furthest pure-forward steps
		// Ships have no min path AP; every reachable cell is a valid endpoint.
		{ 0, 128, 128, 4 },
		{ 1, 81, 81, 5 },
		{ 2, 38, 38, 7 },
		{ 3, 25, 25, 7 },
	};

	/// <summary>First N forward steps in a path are free; N equals current momentum.</summary>
	public static int FreeForwardStepsAt(int momentum) => momentum;

	public static int ForwardStepApCost(int forwardStepsInPath, int momentumAtStep) =>
		forwardStepsInPath < FreeForwardStepsAt(momentumAtStep)
			? 0
			: ForwardCostAfterFree;

	public static int TotalApForPureForwardPath(int startMomentum, int stepCount)
	{
		var buildup = new MomentumConfig.Buildup(startMomentum, 0);
		var forwardSteps = 0;
		var totalAp = 0;

		for (var step = 0; step < stepCount; step++)
		{
			totalAp += ForwardStepApCost(forwardSteps, buildup.Level);

			forwardSteps++;
			buildup = MomentumConfig.ApplyMovementStep(
				buildup,
				ESpatialOrientation.Forward,
				startMomentum,
				momentumGainedFromMovementThisTurn: 0);
		}

		return totalAp;
	}

	public static int MomentumAfterPureForwardPath(int startMomentum, int stepCount) =>
		MomentumConfig.MomentumAfterPureForwardPath(startMomentum, stepCount);

	/// <summary>Ship move endpoints may spend any AP amount (including free paths).</summary>
	public static bool IsValidMoveEndpoint(int totalApSpent) => totalApSpent >= 0;

	public static MovePathSession PureForwardMove(
		string actorId,
		Coord origin,
		int stepCount,
		int startMomentum)
	{
		var pathApSpent = TotalApForPureForwardPath(startMomentum, stepCount);
		return BattleTestFixture.ForwardPath(actorId, origin, stepCount, pathApSpent);
	}
}
