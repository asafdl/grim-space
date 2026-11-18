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
public sealed class SearchBridgeBehaviorTests
{
	private const string PlayerId = "player";

	public static TheoryData<int[][], int[]> VisitBudgets => new()
	{
		{ [[3, 1], [3, 1]], [0] },
		{ [[4, 2], [3, 1]], [0] },
		{ [[4, 1], [1, 4]], [0, 1] },
		{ [[2, 1], [1, 2], [3, 3]], [0, 1, 2] },
		{ [[4, 1], [1, 4], [3, 3]], [0, 1, 2] },
	};

	[Theory]
	[MemberData(nameof(VisitBudgets))]
	public void GoDominancePreservesIncomparableBudgets(int[][] budgets, int[] expectedPositions)
	{
		var battle = BattleTestFixture.BeginSimulation(
			BattleTestFixture.Player(Coord.Zero, actionPoints: budgets.Length),
			BattleTestFixture.Enemy(new Coord(0, 0, budgets.Length)),
			new GrimSpace.Math.Grid.Grid(1, 1, budgets.Length + 1));
		var input = new SearchInput<BattleWorld, ActorRuntime>(
			(simulation, actorId) => new SearchVisitState("shared-key",
				budgets[simulation.World.StateOf(actorId).Position.Z]));

		var positions = SearchBridge.Enumerate(battle.Engine.CreateSimulation(), PlayerId,
				new[] { MoveDef.Instance }, input, TestContext.Current.CancellationToken)
			.Select(frame => frame.World.StateOf(PlayerId).Position.Z)
			.ToArray();

		Assert.Equal(expectedPositions, positions);
	}

	[Fact]
	public void PruneChildrenOnRootStopsAllDescendants()
	{
		var battle = BattleTestFixture.BeginSimulation(new Coord(5, 5, 5));
		var depths = new List<int>();

		foreach (var frame in SearchBridge.Enumerate(
			battle.PlayerAgent.Sim,
			PlayerId,
			Capabilities.Movement,
			new SearchInput<BattleWorld, ActorRuntime>(BattleSearchVisit.ForCapabilities),
			TestContext.Current.CancellationToken))
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

		foreach (var frame in SearchBridge.Enumerate(
			battle.PlayerAgent.Sim,
			PlayerId,
			Capabilities.Movement,
			new SearchInput<BattleWorld, ActorRuntime>(BattleSearchVisit.ForCapabilities),
			TestContext.Current.CancellationToken))
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

		foreach (var _ in SearchBridge.Enumerate(
			battle.PlayerAgent.Sim,
			PlayerId,
			Capabilities.Movement,
			new SearchInput<BattleWorld, ActorRuntime>(BattleSearchVisit.ForCapabilities),
			TestContext.Current.CancellationToken))
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
		var maxDepth = SearchBridge.Enumerate(
			battle.PlayerAgent.Sim,
			PlayerId,
			Capabilities.Movement,
			new SearchInput<BattleWorld, ActorRuntime>(BattleSearchVisit.ForCapabilities),
			TestContext.Current.CancellationToken).Max(frame => frame.Depth);

		Assert.True(maxDepth > 1);
	}

	[Fact]
	public void ActionFilterRejectsCandidatesBeforeTheyAreEnqueued()
	{
		var battle = OneApBattle();
		var frames = SearchBridge.Enumerate(
				battle.PlayerAgent.Sim,
				PlayerId,
				[MoveDef.Instance],
				new SearchInput<BattleWorld, ActorRuntime>(
					BattleSearchVisit.ForCapabilities,
					ShouldExploreAction: (_, _, action) =>
						action is MoveStepAction { Direction: ESpatialOrientation.Forward }),
				TestContext.Current.CancellationToken)
			.ToList();

		Assert.Equal(2, frames.Count);
		Assert.Empty(frames[0].Actions);
		Assert.All(
			frames[1].Actions,
			action => Assert.IsType<MoveStepAction>(action));
	}

	[Fact]
	public void PrioritizedSearchYieldsEveryMovementBranchBeforeRemainingBranches()
	{
		var battle = OneApBattle();
		IReadOnlyList<IActionDef<IAction, BattleWorld, ActorRuntime, IEffect<BattleWorld, ActorRuntime>>> allDefs =
		[
			..Capabilities.Movement,
			LightningCannonDef.Instance,
		];

		var frames = SearchBridge.Enumerate(
			battle.PlayerAgent.Sim,
			PlayerId,
			allDefs,
			new SearchInput<BattleWorld, ActorRuntime>(
				BattleSearchVisit.ForCapabilities,
				IsMovementBranch),
			TestContext.Current.CancellationToken).ToList();
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
			Assert.Contains(frame.Actions, action => action is LightningCannonAction);
		});
	}

	[Fact]
	public void PrioritizedSearchPreservesTheFullSearchFrameSet()
	{
		var battle = OneApBattle();
		IReadOnlyList<IActionDef<IAction, BattleWorld, ActorRuntime, IEffect<BattleWorld, ActorRuntime>>> allDefs =
		[
			..Capabilities.Movement,
			LightningCannonDef.Instance,
		];
		var expected = SearchBridge.Enumerate(
				battle.PlayerAgent.Sim,
				PlayerId,
				allDefs,
				new SearchInput<BattleWorld, ActorRuntime>(BattleSearchVisit.ForCapabilities),
				TestContext.Current.CancellationToken)
			.Select(FrameKey)
			.ToHashSet();

		var actual = SearchBridge.Enumerate(
				battle.PlayerAgent.Sim,
				PlayerId,
				allDefs,
				new SearchInput<BattleWorld, ActorRuntime>(
					BattleSearchVisit.ForCapabilities,
					IsMovementBranch),
				TestContext.Current.CancellationToken)
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
			LightningCannonDef.Instance,
		];
		var priorityCount = SearchBridge.Enumerate(
			battle.PlayerAgent.Sim,
			PlayerId,
			Capabilities.Movement,
			new SearchInput<BattleWorld, ActorRuntime>(BattleSearchVisit.ForCapabilities),
			TestContext.Current.CancellationToken).Count();
		using var frames = SearchBridge.Enumerate(
				battle.PlayerAgent.Sim,
				PlayerId,
				allDefs,
				new SearchInput<BattleWorld, ActorRuntime>(
					BattleSearchVisit.ForCapabilities,
					IsMovementBranch),
				TestContext.Current.CancellationToken).GetEnumerator();

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
			LightningCannonDef.Instance,
		];
		var frames = new List<SearchFrame<BattleWorld, ActorRuntime>>();

		foreach (var frame in SearchBridge.Enumerate(
			battle.PlayerAgent.Sim,
			PlayerId,
			allDefs,
			new SearchInput<BattleWorld, ActorRuntime>(
				BattleSearchVisit.ForCapabilities,
				IsMovementBranch),
			TestContext.Current.CancellationToken))
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
			LightningCannonDef.Instance,
		];
		IReadOnlyList<IActionDef<IAction, BattleWorld, ActorRuntime, IEffect<BattleWorld, ActorRuntime>>> prioritizedDefs =
		[
			prioritizedMove,
			HeadingDef.Instance,
			RollDef.Instance,
			LightningCannonDef.Instance,
		];

		_ = SearchBridge.Enumerate(
			OneApBattle().PlayerAgent.Sim,
			PlayerId,
			normalDefs,
			new SearchInput<BattleWorld, ActorRuntime>(BattleSearchVisit.ForCapabilities),
			TestContext.Current.CancellationToken).Count();
		_ = SearchBridge.Enumerate(
			OneApBattle().PlayerAgent.Sim,
			PlayerId,
			prioritizedDefs,
			new SearchInput<BattleWorld, ActorRuntime>(
				BattleSearchVisit.ForCapabilities,
				IsMovementBranch),
			TestContext.Current.CancellationToken).Count();

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

