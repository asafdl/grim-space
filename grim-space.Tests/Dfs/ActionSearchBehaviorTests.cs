using GrimSpace.Battle;
using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Ai;
using GrimSpace.Battle.Abilities;
using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.Units;
using GrimSpace.Battle.World;
using GrimSpace.Core.Actions;
using GrimSpace.Core.Dfs;
using GrimSpace.Math.Grid;

namespace GrimSpace.Tests.Dfs;

[BattleTestSuite]
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
			Capabilities.Movement,
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
			Capabilities.Movement,
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
			Capabilities.Movement,
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
			Capabilities.Movement,
			BattleSearchVisit.ForCapabilities).Max(frame => frame.Depth);

		Assert.True(maxDepth > 1);
	}

	[Fact]
	public void PrioritizedSearchYieldsEveryMovementBranchBeforeRemainingBranches()
	{
		var battle = OneApBattle();
		IReadOnlyList<IActionDef<IAction, BattleWorld, ActorRuntime, IEffect<BattleWorld, ActorRuntime>>> allDefs =
		[
			..Capabilities.Movement,
			RailgunDef.Instance,
		];

		var frames = ActionSearch.Run(
			battle.PlayerAgent.Sim,
			PlayerId,
			allDefs,
			new SearchInput<BattleWorld, ActorRuntime>(
				BattleSearchVisit.ForCapabilities,
				IsMovementBranch)).ToList();
		var firstRemaining = frames.FindIndex(frame => frame.Phase == SearchFramePhase.Remaining);

		Assert.True(firstRemaining > 0);
		Assert.All(frames.Take(firstRemaining), frame =>
		{
			Assert.Equal(SearchFramePhase.Priority, frame.Phase);
			Assert.All(frame.Actions, action =>
				Assert.True(action is MoveStepAction or HeadingTurnAction or RollAction));
		});
		Assert.All(frames.Skip(firstRemaining), frame =>
		{
			Assert.Equal(SearchFramePhase.Remaining, frame.Phase);
			Assert.Contains(frame.Actions, action => action is RailgunAction);
		});
	}

	[Fact]
	public void PrioritizedSearchPreservesTheFullSearchFrameSet()
	{
		var battle = OneApBattle();
		IReadOnlyList<IActionDef<IAction, BattleWorld, ActorRuntime, IEffect<BattleWorld, ActorRuntime>>> allDefs =
		[
			..Capabilities.Movement,
			RailgunDef.Instance,
		];
		var expected = ActionSearch.Run(
				battle.PlayerAgent.Sim,
				PlayerId,
				allDefs,
				BattleSearchVisit.ForCapabilities)
			.Select(FrameKey)
			.ToHashSet();

		var actual = ActionSearch.Run(
				battle.PlayerAgent.Sim,
				PlayerId,
				allDefs,
				new SearchInput<BattleWorld, ActorRuntime>(
					BattleSearchVisit.ForCapabilities,
					IsMovementBranch))
			.Select(FrameKey)
			.ToHashSet();

		Assert.Equal(expected, actual);
	}

	[Fact]
	public void PriorityPhaseDefersRemainingBranchesUntilMovementIsExhausted()
	{
		var battle = OneApBattle();
		IReadOnlyList<IActionDef<IAction, BattleWorld, ActorRuntime, IEffect<BattleWorld, ActorRuntime>>> allDefs =
		[
			..Capabilities.Movement,
			RailgunDef.Instance,
		];
		var priorityCount = ActionSearch.Run(
			battle.PlayerAgent.Sim,
			PlayerId,
			Capabilities.Movement,
			BattleSearchVisit.ForCapabilities).Count();
		using var frames = ActionSearch.Run(
				battle.PlayerAgent.Sim,
				PlayerId,
				allDefs,
				new SearchInput<BattleWorld, ActorRuntime>(
					BattleSearchVisit.ForCapabilities,
					IsMovementBranch)).GetEnumerator();

		for (var i = 0; i < priorityCount; i++)
		{
			Assert.True(frames.MoveNext());
			Assert.Equal(SearchFramePhase.Priority, frames.Current.Phase);
		}

		Assert.True(frames.MoveNext());
		Assert.Equal(SearchFramePhase.Remaining, frames.Current.Phase);
	}

	[Fact]
	public void PruningPriorityRootStopsRemainingPhase()
	{
		var battle = OneApBattle();
		IReadOnlyList<IActionDef<IAction, BattleWorld, ActorRuntime, IEffect<BattleWorld, ActorRuntime>>> allDefs =
		[
			..Capabilities.Movement,
			RailgunDef.Instance,
		];
		var frames = new List<SearchFrame<BattleWorld, ActorRuntime>>();

		foreach (var frame in ActionSearch.Run(
			battle.PlayerAgent.Sim,
			PlayerId,
			allDefs,
			new SearchInput<BattleWorld, ActorRuntime>(
				BattleSearchVisit.ForCapabilities,
				IsMovementBranch)))
		{
			frames.Add(frame);
			frame.PruneChildren = true;
		}

		var root = Assert.Single(frames);
		Assert.Equal(SearchFramePhase.Priority, root.Phase);
		Assert.Equal(0, root.Depth);
	}

	[Fact]
	public void PrioritizedSearchDoesNotRetraverseMovementBranches()
	{
		var normalMove = new CountingActionDef(MoveDef.Instance);
		var prioritizedMove = new CountingActionDef(MoveDef.Instance);
		IReadOnlyList<IActionDef<IAction, BattleWorld, ActorRuntime, IEffect<BattleWorld, ActorRuntime>>> normalDefs =
		[
			normalMove,
			HeadingDef.Instance,
			RollDef.Instance,
			RailgunDef.Instance,
		];
		IReadOnlyList<IActionDef<IAction, BattleWorld, ActorRuntime, IEffect<BattleWorld, ActorRuntime>>> prioritizedDefs =
		[
			prioritizedMove,
			HeadingDef.Instance,
			RollDef.Instance,
			RailgunDef.Instance,
		];

		_ = ActionSearch.Run(
			OneApBattle().PlayerAgent.Sim,
			PlayerId,
			normalDefs,
			BattleSearchVisit.ForCapabilities).Count();
		_ = ActionSearch.Run(
			OneApBattle().PlayerAgent.Sim,
			PlayerId,
			prioritizedDefs,
			new SearchInput<BattleWorld, ActorRuntime>(
				BattleSearchVisit.ForCapabilities,
				IsMovementBranch)).Count();

		Assert.Equal(normalMove.DiscoverCalls, prioritizedMove.DiscoverCalls);
	}

	private static BattleOrchestrator OneApBattle()
	{
		var origin = new Coord(5, 5, 5);
		return BattleTestFixture.BeginSimulation(
			BattleTestFixture.Player(origin, actionPoints: 1),
			BattleTestFixture.Enemy(origin + Coord.Forward * 6));
	}

	private static string FrameKey(SearchFrame<BattleWorld, ActorRuntime> frame) =>
		string.Join('|', frame.Actions);

	private static bool IsMovementBranch(IReadOnlyList<IAction> actions) =>
		actions.All(action => action is MoveStepAction or HeadingTurnAction or RollAction);

	private sealed class CountingActionDef(
		IActionDef<IAction, BattleWorld, ActorRuntime, IEffect<BattleWorld, ActorRuntime>> inner)
		: IActionDef<IAction, BattleWorld, ActorRuntime, IEffect<BattleWorld, ActorRuntime>>
	{
		public int DiscoverCalls { get; private set; }

		public IEnumerable<IAction> Discover(BattleWorld world, ActorRuntime runtime, string actorId)
		{
			DiscoverCalls++;
			return inner.Discover(world, runtime, actorId);
		}

		public bool IsPossible(IAction action, BattleWorld world, ActorRuntime runtime) =>
			inner.IsPossible(action, world, runtime);

		public bool IsLegal(IAction action, BattleWorld world, ActorRuntime runtime) =>
			inner.IsLegal(action, world, runtime);

		public IReadOnlyList<IEffect<BattleWorld, ActorRuntime>> Resolve(
			IAction action,
			BattleWorld world,
			ActorRuntime runtime) =>
			inner.Resolve(action, world, runtime);
	}
}

[BattleTestSuite]
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
