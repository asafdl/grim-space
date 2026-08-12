using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Ai;
using GrimSpace.Core.Actions;
using GrimSpace.Core.Dfs;
using GrimSpace.Math.Grid;

namespace GrimSpace.Tests.Dfs;

public sealed class ActionSearchBehaviorTests
{
	private const string PlayerId = "player";

	[Fact]
	public void PruneChildrenOnRootStopsAllDescendants()
	{
		var battle = BattleTestFixture.BeginSimulation(new Coord(5, 5, 5));
		var depths = new List<int>();

		foreach (var frame in ActionSearch.Run(
			battle.PlayerAgent.Sim,
			PlayerId,
			[MoveDef.Instance],
			BattleSearchVisit.ForCapabilities))
		{
			depths.Add(frame.Depth);
			if (frame.Depth == 0)
				frame.PruneChildren = true;
		}

		Assert.Equal([0], depths);
	}

	[Fact]
	public void PruneChildrenOnlyStopsCurrentBranch()
	{
		var battle = BattleTestFixture.BeginSimulation(new Coord(5, 5, 5));
		var prunedBranchChildSeen = false;
		var siblingDescendantSeen = false;
		IAction? prunedFirstAction = null;
		var startDepth = battle.PlayerAgent.Sim.Actions.Count;

		foreach (var frame in ActionSearch.Run(
			battle.PlayerAgent.Sim,
			PlayerId,
			[MoveDef.Instance],
			BattleSearchVisit.ForCapabilities))
		{
			if (frame.Depth == 1 && prunedFirstAction is null)
			{
				prunedFirstAction = frame.Actions[startDepth];
				frame.PruneChildren = true;
				continue;
			}

			if (frame.Depth <= 1 || prunedFirstAction is null)
				continue;

			if (ReferenceEquals(frame.Actions[startDepth], prunedFirstAction))
				prunedBranchChildSeen = true;
			else
				siblingDescendantSeen = true;
		}

		Assert.NotNull(prunedFirstAction);
		Assert.False(prunedBranchChildSeen);
		Assert.True(siblingDescendantSeen);
	}

	[Fact]
	public void BreakingEnumerationStopsSearch()
	{
		var battle = BattleTestFixture.BeginSimulation(new Coord(5, 5, 5));
		var count = 0;

		foreach (var _ in ActionSearch.Run(
			battle.PlayerAgent.Sim,
			PlayerId,
			[MoveDef.Instance],
			BattleSearchVisit.ForCapabilities))
		{
			count++;
			if (count == 3)
				break;
		}

		Assert.Equal(3, count);
	}

	[Fact]
	public void LeavingPruneChildrenFalsePreservesDescendants()
	{
		var battle = BattleTestFixture.BeginSimulation(new Coord(5, 5, 5));
		var maxDepth = ActionSearch.Run(
			battle.PlayerAgent.Sim,
			PlayerId,
			[MoveDef.Instance],
			BattleSearchVisit.ForCapabilities).Max(frame => frame.Depth);

		Assert.True(maxDepth > 1);
	}
}

public sealed class BudgetFrontierTests
{
	[Fact]
	public void ExactDuplicateBudgetIsPruned()
	{
		var frontier = new List<int[]> { new[] { 3, 1 } };

		Assert.True(BudgetFrontier.ShouldPrune(frontier, [3, 1]));
		Assert.Single(frontier);
	}

	[Fact]
	public void DominatingBudgetPrunesInferiorVisit()
	{
		var frontier = new List<int[]> { new[] { 4, 2 } };

		Assert.True(BudgetFrontier.ShouldPrune(frontier, [3, 1]));
		Assert.Equal(new[] { 4, 2 }, frontier.Single());
	}

	[Fact]
	public void IncomparableBudgetsBothSurvive()
	{
		var frontier = new List<int[]> { new[] { 4, 1 } };

		Assert.False(BudgetFrontier.ShouldPrune(frontier, [1, 4]));
		Assert.Equal(2, frontier.Count);
		Assert.Contains(frontier, vector => vector.SequenceEqual(new[] { 4, 1 }));
		Assert.Contains(frontier, vector => vector.SequenceEqual(new[] { 1, 4 }));
	}

	[Fact]
	public void NewDominantVectorRemovesDominatedEntries()
	{
		var frontier = new List<int[]> { new[] { 2, 1 }, new[] { 1, 2 } };

		Assert.False(BudgetFrontier.ShouldPrune(frontier, [3, 3]));
		Assert.Single(frontier);
		Assert.Equal(new[] { 3, 3 }, frontier[0]);
	}

	[Fact]
	public void DoesNotCreateSyntheticComponentWiseMaximum()
	{
		var frontier = new List<int[]> { new[] { 4, 1 } };
		Assert.False(BudgetFrontier.ShouldPrune(frontier, [1, 4]));

		Assert.DoesNotContain(frontier, vector => vector.SequenceEqual(new[] { 4, 4 }));
		Assert.Equal(2, frontier.Count);
	}
}
