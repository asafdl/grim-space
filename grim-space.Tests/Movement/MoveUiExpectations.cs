using GrimSpace.Battle;
using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Movement;
using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.World;
using GrimSpace.Core.Actions;
using GrimSpace.Core.Engine;
using GrimSpace.Math.Grid;

namespace GrimSpace.Tests.Movement;

/// <summary>
/// Independent index build used to verify the turn-scoped cache matches a fresh turn-start search.
/// </summary>
internal static class MoveUiExpectations
{
	public static MoveOptionIndex CaptureTurnStartIndex(BattleOrchestrator battle) =>
		MoveOptionIndex.FromSimulation(battle.Sim, battle.PlayerId);

	public static IReadOnlyList<MovePathSession> FromIndex(
		MoveOptionIndex index,
		IReadOnlyList<IAction> committed) =>
		index.GetPaths(committed);

	public static IReadOnlyList<MovePathSession> FromFreshSearch(
		Simulation<BattleWorld, ActorRuntime> sim,
		string actorId,
		IReadOnlyList<IAction> committed) =>
		MoveOptionIndex.FromSimulation(sim, actorId).GetPaths(committed);

	public static void AssertEquivalent(
		IReadOnlyList<MovePathSession> expected,
		IReadOnlyList<MovePathSession> actual,
		int baselinePathApSpent = 0)
	{
		var expectedByEnd = expected.ToDictionary(path => path.EndPosition);
		var actualByEnd = actual.ToDictionary(path => path.EndPosition);

		Assert.Equal(
			expectedByEnd.Keys.OrderBy(coord => coord.X).ThenBy(coord => coord.Y).ThenBy(coord => coord.Z),
			actualByEnd.Keys.OrderBy(coord => coord.X).ThenBy(coord => coord.Y).ThenBy(coord => coord.Z));

		foreach (var end in expectedByEnd.Keys)
		{
			Assert.Equal(
				expectedByEnd[end].ExtensionApCost(baselinePathApSpent),
				actualByEnd[end].ExtensionApCost(baselinePathApSpent));
			Assert.Equal(expectedByEnd[end].Cells, actualByEnd[end].Cells);
		}
	}
}
