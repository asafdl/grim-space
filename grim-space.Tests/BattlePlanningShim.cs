using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Movement;
using GrimSpace.Battle.Units;
using GrimSpace.Tests;

namespace GrimSpace.Battle.Planning;

internal static class Preview
{
	public static PreviewActor Simulate(TestPlan plan) => new(plan.Board.StateOf(plan.OwnerId));

	public static IReadOnlyList<Option> GetLegalMoves(TestPlan plan) =>
		ActionQueries.EnumerateMovePaths(plan.Board, plan.Runtime, plan.OwnerId).ToList();
}

internal readonly record struct PreviewActor(State Actor);
