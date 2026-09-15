using GrimSpace.Battle.Movement;
using GrimSpace.Math.Grid;

namespace GrimSpace.Tests.Movement;

internal static class MovementExpectations
{
	public const int FighterApPerTurn = 4;

	public static MovePathSession PureForwardMove(
		string actorId,
		Coord origin,
		int stepCount) =>
		BattleTestFixture.ForwardPath(actorId, origin, stepCount, stepCount);
}
