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

	public static IReadOnlyList<Option> FromIndex(MoveOptionIndex index, IReadOnlyList<IAction> committed) =>
		index.GetOptions(committed);

	public static IReadOnlyList<Option> FromFreshSearch(
		Simulation<BattleWorld, ActorRuntime> sim,
		string actorId,
		IReadOnlyList<IAction> committed) =>
		MoveOptionIndex.FromSimulation(sim, actorId).GetOptions(committed);

	public static void AssertEquivalent(IReadOnlyList<Option> expected, IReadOnlyList<Option> actual)
	{
		var expectedByEnd = expected.ToDictionary(option => option.EndPosition);
		var actualByEnd = actual.ToDictionary(option => option.EndPosition);

		Assert.Equal(
			expectedByEnd.Keys.OrderBy(coord => coord.X).ThenBy(coord => coord.Y).ThenBy(coord => coord.Z),
			actualByEnd.Keys.OrderBy(coord => coord.X).ThenBy(coord => coord.Y).ThenBy(coord => coord.Z));

		foreach (var end in expectedByEnd.Keys)
		{
			Assert.Equal(expectedByEnd[end].ApCost, actualByEnd[end].ApCost);
			Assert.Equal(expectedByEnd[end].Path, actualByEnd[end].Path);
		}
	}
}
