using System.Diagnostics;
using System.Text.Json;
using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Ai;
using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.Units;
using GrimSpace.Battle.World;
using GrimSpace.Core.Actions;
using GrimSpace.Core.Dfs;
using GrimSpace.Core.Engine;
using GrimSpace.Math.Grid;
using GrimSpace.Run.Persistence;
using BattleDef = GrimSpace.Core.Actions.IActionDef<GrimSpace.Core.Actions.IAction, GrimSpace.Battle.World.BattleWorld, GrimSpace.Battle.Runtime.ActorRuntime, GrimSpace.Core.Actions.IEffect<GrimSpace.Battle.World.BattleWorld, GrimSpace.Battle.Runtime.ActorRuntime>>;

namespace GrimSpace.Tests.Dfs;

[BattleTestSuite]
public sealed class SearchBridgeTests(ITestOutputHelper output)
{
	private readonly PersistenceRegistry _registry = PersistenceRegistry.CreateDefault();

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public void MovementMatchesOrderedPathsAndCompleteWorlds(bool translationsFirst)
	{
		var source = CreateSource();
		var sawHeading = false;
		var sawRoll = false;
		AssertMatches(source, Capabilities.Movement, BattleSearchVisit.ForMovePreview,
			translationsFirst ? Translations(source.Actions.Count) : null, inspect: frame =>
		{
			sawHeading |= frame.Actions.Any(action => action is HeadingTurnAction);
			sawRoll |= frame.Actions.Any(action => action is RollAction);
		});
		Assert.True(sawHeading);
		Assert.True(sawRoll);
	}

	[Theory]
	[InlineData(false, false)]
	[InlineData(true, false)]
	[InlineData(true, true)]
	public void MixedSearchPreservesDamageKillsHeatAndScheduling(bool movementFirst, bool lethal)
	{
		var source = CreateSource(heat: 90, lethal: lethal);
		BattleDef[] definitions = [.. Capabilities.Movement, LightningCannonDef.Instance];
		var targetHull = source.World.StateOf("enemy").CurrentHullPoints;
		var targetShields = Enum.GetValues<ESpatialOrientation>()
			.Sum(face => source.World.StateOf("enemy").CurrentShieldPoints[face]);
		var sawDamage = false;
		var sawShieldDamage = false;
		var sawKill = false;
		var sawMixedPath = false;
		var sawPending = false;
		var sawRemaining = false;
		AssertMatches(source, definitions, BattleSearchVisit.ForCapabilities,
			movementFirst ? Movement : null, inspect: frame =>
		{
			var target = frame.World.StateOf("enemy");
			sawDamage |= target.CurrentHullPoints < targetHull;
			sawShieldDamage |= Enum.GetValues<ESpatialOrientation>()
				.Sum(face => target.CurrentShieldPoints[face]) < targetShields;
			sawKill |= !target.IsAlive;
			sawMixedPath |= frame.Actions.Any(action => action is MoveStepAction)
				&& frame.Actions.Any(action => action is LightningCannonAction);
			sawPending |= frame.World.Timeline.ContainsPending(action => action is EnterOverheatAction);
			sawRemaining |= frame.Phase == SearchFramePhase.Remaining;
		});
		Assert.True(sawDamage);
		Assert.Equal(!lethal, sawShieldDamage);
		Assert.Equal(lethal, sawKill);
		Assert.True(sawMixedPath);
		Assert.True(sawPending);
		Assert.True(sawRemaining);
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public void QueuedPrefixAndInitialPriorityPhaseMatch(bool attackPrefix)
	{
		var source = CreateSource(ap: 2);
		Assert.True(source.TryEnqueue(
			attackPrefix ? new LightningCannonAction("player") : new MoveStepAction("player")));
		BattleDef[] definitions = [.. Capabilities.Movement, LightningCannonDef.Instance];
		AssertMatches(source, definitions, BattleSearchVisit.ForCapabilities, Movement);
	}

	[Theory]
	[InlineData(0, 0)]
	[InlineData(1, 0)]
	[InlineData(-1, 7)]
	public void ConsumerPruningAndEarlyStopMatch(int pruneDepth, int stopAfter)
	{
		var source = CreateSource();
		BattleDef[] definitions = [.. Capabilities.Movement, LightningCannonDef.Instance];
		var count = AssertMatches(source, definitions, BattleSearchVisit.ForCapabilities,
			Movement, pruneDepth, stopAfter);
		if (pruneDepth == 0)
			Assert.Equal(1, count);
		if (stopAfter != 0)
			Assert.Equal(stopAfter, count);
	}

	[Fact]
	public void SearchIncludesDepthTwentyButDoesNotEmitDepthTwentyOne()
	{
		var battle = BattleTestFixture.BeginSimulation(
			BattleTestFixture.Player(Coord.Zero),
			BattleTestFixture.Enemy(new Coord(0, 0, 31)),
			new GrimSpace.Math.Grid.Grid(1, 1, 32));
		battle.Engine.World.StateOf("player").CurrentActionPoints = 25;
		var source = battle.Engine.CreateSimulation();
		var maxDepth = 0;
		var count = AssertMatches(source, [MoveDef.Instance], BattleSearchVisit.ForMovePreview,
			null, inspect: frame => maxDepth = System.Math.Max(maxDepth, frame.Depth));
		Assert.Equal(20, maxDepth);
		Assert.Equal(21, count);
	}

	[Fact]
	public void CallbackExceptionsSurfaceInsteadOfReturningPartialSuccess()
	{
		var source = CreateSource();
		var error = Assert.Throws<InvalidOperationException>(() =>
			SearchBridge.Run(source, "player", Capabilities.Movement, MovePreviewInput,
				decide: _ => throw new InvalidOperationException("decision failed")));
		Assert.Equal("decision failed", error.Message);
	}

	[Fact]
	public void IncrementalSearchDoesNotExploreAheadAndCanBeDisposedAtAYield()
	{
		var source = CreateSource();
		var before = Capture(source.World);
		var visits = 0;
		var discoveries = 0;
		var input = new SearchInput<BattleWorld, ActorRuntime>(
			(simulation, actorId) =>
			{
				visits++;
				return BattleSearchVisit.ForMovePreview(simulation, actorId);
			},
			ShouldExploreAction: (_, _, _) =>
			{
				discoveries++;
				return true;
			});
		var frames = SearchBridge.Enumerate(source, "player", Capabilities.Movement, input,
			TestContext.Current.CancellationToken);
		using var cursor = frames.GetEnumerator();
		Assert.Equal(0, visits);
		Assert.Equal(0, discoveries);

		Assert.True(cursor.MoveNext());
		Assert.Equal(0, cursor.Current.Depth);
		Assert.Equal(1, visits);
		Assert.Equal(0, discoveries);
		cursor.Dispose();

		Assert.False(cursor.MoveNext());
		Assert.Equal(1, visits);
		Assert.Equal(0, discoveries);
		Assert.Equal(before, Capture(source.World));
	}

	[Fact]
	public void IncrementalSearchPropagatesCancellationBetweenYields()
	{
		var source = CreateSource();
		using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
		using var cursor = SearchBridge.Enumerate(
			source, "player", Capabilities.Movement, MovePreviewInput, cancellation.Token).GetEnumerator();
		Assert.True(cursor.MoveNext());
		cancellation.Cancel();

		Assert.Throws<OperationCanceledException>(() => cursor.MoveNext());
		Assert.Empty(source.Actions);
	}

	[Fact]
	public void IncrementalSearchPropagatesCallbackFailureAfterAYield()
	{
		var source = CreateSource();
		var failure = new InvalidOperationException("incremental discovery failed");
		var input = new SearchInput<BattleWorld, ActorRuntime>(
			BattleSearchVisit.ForMovePreview,
			ShouldExploreAction: (_, _, _) => throw failure);
		using var cursor = SearchBridge.Enumerate(
			source, "player", Capabilities.Movement, input, TestContext.Current.CancellationToken).GetEnumerator();
		Assert.True(cursor.MoveNext());

		Assert.Same(failure, Assert.Throws<InvalidOperationException>(() => cursor.MoveNext()));
		Assert.Empty(source.Actions);
	}

	[Fact]
	public void AiCapabilitiesWithSpawnsQueuedPrefixAndPendingTimelineMatch()
	{
		var source = CreateSource(ap: 2, heat: 90);
		Assert.True(source.TryEnqueue(new LightningCannonAction("player")));
		Assert.True(source.World.Timeline.ToSnapshot().Pending.Count != 0);
		var definitions = AiController.SearchCapabilities(
			source.World.StateOf("player"), source.RuntimeFor("player"));
		Assert.Contains(definitions, definition => definition is VoidBombDef);
		AssertMatches(source, definitions, BattleSearchVisit.ForCapabilities, null,
			shouldExplore: EnemySearchInput.ShouldExploreAction);
	}

	[Fact]
	public void ParallelConsumerControlIsRejectedRatherThanIgnored()
	{
		Assert.Throws<NotSupportedException>(() =>
			SearchBridge.Run(CreateSource(), "player", Capabilities.Movement, MovePreviewInput,
				decide: _ => BridgeDecision.Stop, workers: 2));
	}

	[Theory]
	[InlineData(0)]
	[InlineData(4)]
	public void DominancePruningReducesMovementVisitsComparedWithUniqueKeys(int workers)
	{
		var source = CreateSource(ap: 4);
		var unprunedVisits = 0;
		var prunedVisits = 0;
		var unpruned = SearchBridge.Run(
			source, "player", Capabilities.Movement,
			new SearchInput<BattleWorld, ActorRuntime>((simulation, actorId) =>
			{
				Interlocked.Increment(ref unprunedVisits);
				var visit = BattleSearchVisit.ForCapabilities(simulation, actorId);
				return new SearchVisitState(new object(), visit.Budget);
			}), workers: workers);
		var pruned = SearchBridge.Run(
			source, "player", Capabilities.Movement,
			new SearchInput<BattleWorld, ActorRuntime>((simulation, actorId) =>
			{
				Interlocked.Increment(ref prunedVisits);
				return BattleSearchVisit.ForCapabilities(simulation, actorId);
			}), workers: workers);

		output.WriteLine(
			$"workers={workers}: dominance disabled: visits={unprunedVisits}, frames={unpruned.Count}; "
			+ $"production capability key: visits={prunedVisits}, frames={pruned.Count}");
		Assert.True(prunedVisits < unprunedVisits,
			$"Dominance did not reduce attempted visits: {prunedVisits} vs {unprunedVisits}.");
		Assert.True(pruned.Count < unpruned.Count,
			$"Dominance did not reduce emitted frames: {pruned.Count} vs {unpruned.Count}.");
	}

	[Theory]
	[InlineData(1, false)]
	[InlineData(4, false)]
	[InlineData(8, false)]
	[InlineData(4, true)]
	public void BoundedSearchMatchesBestExhaustiveScoreAndReturnsOnlyWinner(int workers, bool queuedPrefix)
	{
		const string actorId = "enemy";
		var source = BattleTestFixture.LightningCannonDuel(stepsBeyondReach: 1, actionPoints: 2);
		if (queuedPrefix)
			Assert.True(source.TryEnqueue(new MoveStepAction(actorId)));
		var before = Capture(source.World);
		BattleDef[] definitions = [.. Capabilities.Movement, LightningCannonDef.Instance];
		var input = new SearchInput<BattleWorld, ActorRuntime>(BattleSearchVisit.ForCapabilities);
		var expected = ActionSearchExhaustive.Run(source, actorId, definitions)
			.Max(frame => EnemySearchInput.ScoreHeuristic(frame, source, actorId, source.Actions.Count));
		var bridge = SearchBridge.FindBest(source, actorId, definitions, input,
			EnemySearchInput.CreateEvaluator(source, actorId), workers, TestContext.Current.CancellationToken);

		Assert.Single(bridge.Frames);
		Assert.Equal(expected, bridge.BestScore);
		Assert.Equal(expected, EnemySearchInput.ScoreHeuristic(bridge.FrameAt(0), source, actorId, source.Actions.Count));
		Assert.True(bridge.BoundPrunedFrames > 0);
		Assert.False(bridge.BudgetExhausted);
		Assert.Equal(before, Capture(source.World));
	}

	[Theory]
	[InlineData(1)]
	[InlineData(4)]
	public void BoundedSearchBudgetReturnsBestEvaluatedPlanWithoutChangingSource(int workers)
	{
		var source = CreateSource(ap: 4);
		Assert.True(source.TryEnqueue(new MoveStepAction("player")));
		var prefix = source.Actions.ToArray();
		var before = Capture(source.World);
		var timer = Stopwatch.StartNew();
		var bridge = SearchBridge.FindBest(source, "player", Capabilities.Movement, MovePreviewInput,
			simulation =>
			{
				if (simulation.Actions.Count == prefix.Length)
					return new SearchEvaluation(0, 100);
				Thread.Sleep(TimeSpan.FromMilliseconds(250));
				return new SearchEvaluation(10, 100);
			}, workers, TestContext.Current.CancellationToken, timeBudget: TimeSpan.FromMilliseconds(200));

		Assert.True(bridge.BudgetExhausted);
		Assert.Equal(10, bridge.BestScore);
		Assert.Equal(2, bridge.EvaluatedFrames);
		Assert.Single(Assert.Single(bridge.Frames).Path);
		var replay = source.Fork();
		foreach (var action in bridge.Frames[0].Path)
			Assert.True(replay.TryEnqueue(action));
		Assert.Equal(Capture(replay.World), Capture(bridge.FrameAt(0).World));
		Assert.Equal(prefix, source.Actions);
		Assert.Equal(before, Capture(source.World));
		Assert.True(timer.Elapsed < TimeSpan.FromSeconds(2), $"Budgeted search took {timer.Elapsed}.");
	}

	[Fact]
	public void BoundedSearchBudgetCanReturnTheLegalRootBeforeAnyWorkerExpansion()
	{
		var source = CreateSource();
		var bridge = SearchBridge.FindBest(source, "player", Capabilities.Movement, MovePreviewInput,
			_ => new SearchEvaluation(7, 100),
			workers: 4, cancellationToken: TestContext.Current.CancellationToken,
			timeBudget: TimeSpan.FromTicks(1));

		Assert.True(bridge.BudgetExhausted);
		Assert.Equal(7, bridge.BestScore);
		Assert.Equal(1, bridge.EvaluatedFrames);
		Assert.Empty(Assert.Single(bridge.Frames).Path);
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public void BoundedSearchBudgetDoesNotHideCancellationOrScoringFailure(bool cancel)
	{
		using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
		var source = CreateSource();
		var failure = Record.Exception(() =>
			SearchBridge.FindBest(source, "player", Capabilities.Movement, MovePreviewInput,
				_ =>
				{
					if (cancel)
					{
						cancellation.Cancel();
						cancellation.Token.ThrowIfCancellationRequested();
					}
					throw new InvalidOperationException("scoring failed at deadline");
				}, workers: 4, cancellationToken: cancellation.Token,
				timeBudget: TimeSpan.FromTicks(1)));

		if (cancel)
			Assert.IsAssignableFrom<OperationCanceledException>(failure);
		else
			Assert.Equal("scoring failed at deadline", Assert.IsType<InvalidOperationException>(failure).Message);
	}

	[Fact]
	public void BoundedWorkersShareIncumbentAndPruneComparedWithLooseBound()
	{
		var source = CreateSource(ap: 4);
		var input = new SearchInput<BattleWorld, ActorRuntime>(BattleSearchVisit.ForCapabilities);
		var loose = SearchBridge.FindBest(source, "player", Capabilities.Movement, input,
			simulation => new SearchEvaluation(-simulation.World.StateOf("player").CurrentActionPoints, 1),
			workers: 4, cancellationToken: TestContext.Current.CancellationToken);
		var tight = SearchBridge.FindBest(source, "player", Capabilities.Movement, input,
			simulation => new SearchEvaluation(-simulation.World.StateOf("player").CurrentActionPoints, 0),
			workers: 4, cancellationToken: TestContext.Current.CancellationToken);

		Assert.InRange(loose.WorkersUsed, 2, 4);
		Assert.Equal(0, loose.BestScore);
		Assert.Equal(loose.BestScore, tight.BestScore);
		Assert.True(tight.EvaluatedFrames < loose.EvaluatedFrames);
		Assert.True(tight.BoundPrunedFrames > 0);
		Assert.Single(tight.Frames);
		output.WriteLine($"goroutines={loose.WorkersUsed}; loose={loose.EvaluatedFrames}, tight={tight.EvaluatedFrames}");
	}

	[Fact]
	public void BoundedWorkerExceptionsSurfaceInsteadOfReturningPartialSuccess()
	{
		var source = CreateSource();
		var error = Assert.Throws<InvalidOperationException>(() =>
			SearchBridge.FindBest(source, "player", Capabilities.Movement, MovePreviewInput,
				simulation => simulation.Actions.Count == 0
					? new SearchEvaluation(0, 100)
					: throw new InvalidOperationException("worker scoring failed"),
				workers: 4, cancellationToken: TestContext.Current.CancellationToken));
		Assert.Equal("worker scoring failed", error.Message);
	}

	[Fact]
	public void BoundedSearchCancellationDoesNotReturnPartialSuccess()
	{
		using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
		var source = CreateSource();
		Assert.ThrowsAny<OperationCanceledException>(() =>
			SearchBridge.FindBest(source, "player", Capabilities.Movement, MovePreviewInput,
				simulation =>
				{
					if (simulation.Actions.Count > 0)
						cancellation.Cancel();
					return new SearchEvaluation(0, 100);
				}, workers: 4, cancellationToken: cancellation.Token));
	}

	[Theory]
	[InlineData(0, 0)]
	[InlineData(2, 0)]
	[InlineData(4, 0)]
	[InlineData(8, 0)]
	[InlineData(0, 1_000)]
	[InlineData(2, 1_000)]
	[InlineData(4, 1_000)]
	[InlineData(8, 1_000)]
	public void GoSearchMatchesExhaustiveAdvanceThenFireOutcome(int workers, int shields)
	{
		const string actorId = "enemy";
		const int trials = 20;
		var source = BattleTestFixture.LightningCannonDuel(stepsBeyondReach: 1, actionPoints: 1, targetShieldPoints: shields);
		var before = Capture(source.World);
		IAction[] expectedActions = [new MoveStepAction(actorId), new LightningCannonAction(actorId)];

		var miss = source.Fork();
		Assert.True(miss.TryEnqueue(expectedActions[1]));
		Assert.Equal(1_000, miss.World.StateOf("player").CurrentHullPoints);
		Assert.True(miss.TryEnqueue(expectedActions[0]));
		var oracle = source.Fork();
		foreach (var action in expectedActions)
			Assert.True(oracle.TryEnqueue(action));
		Assert.True(oracle.World.StateOf("player").CurrentHullPoints < 1_000
			|| !oracle.World.StateOf("player").CurrentShieldPoints
				.Matches(miss.World.StateOf("player").CurrentShieldPoints));
		var missVisit = BattleSearchVisit.ForCapabilities(miss, actorId);
		var hitVisit = BattleSearchVisit.ForCapabilities(oracle, actorId);
		Assert.NotEqual(hitVisit.State, missVisit.State);
		Assert.Equal(hitVisit.Budget, missVisit.Budget);
		var expectedWorld = Capture(oracle.World);
		BattleDef[] definitions = [.. Capabilities.Movement, LightningCannonDef.Instance];
		var input = new SearchInput<BattleWorld, ActorRuntime>(
			BattleSearchVisit.ForCapabilities, ShouldExploreAction: EnemySearchInput.ShouldExploreAction);
		var exhaustive = Best(ActionSearchExhaustive.Run(source, actorId, definitions));
		Assert.Equal(EnemySearchInput.DamageHitBonus, exhaustive.Score);
		Assert.Equal(expectedActions, exhaustive.Frame.Actions.ToArray());
		Assert.Equal(expectedWorld, Capture(exhaustive.Frame.World));

		var failures = new List<string>();
		for (var trial = 0; trial < trials; trial++)
		{
			var bridge = SearchBridge.Run(source, actorId, definitions, input, workers: workers);
			if (workers > 0)
				Assert.True(bridge.WorkersUsed >= 2, "The fixture must exercise concurrent branches.");
			var frames = Enumerable.Range(0, bridge.Count).Select(bridge.FrameAt).ToArray();
			Assert.Contains(frames, frame => frame.Actions.SequenceEqual(miss.Actions));
			var best = Best(frames);
			var replay = source.Fork();
			foreach (var action in best.Frame.Actions)
				Assert.True(replay.TryEnqueue(action));
			Assert.Equal(Capture(best.Frame.World), Capture(replay.World));
			if (best.Score != exhaustive.Score || Capture(best.Frame.World) != expectedWorld
				|| !best.Frame.Actions.SequenceEqual(expectedActions))
			{
				Assert.Contains(frames, frame => frame.Actions.SequenceEqual(miss.Actions));
				failures.Add($"trial={trial}, score={best.Score}, actions={string.Join(" -> ", best.Frame.Actions)}");
			}
		}
		output.WriteLine($"workers={workers}, shields={shields}: exhaustive oracle selected advance then fire, score={exhaustive.Score}; " +
			$"Go selected the same outcome in {trials - failures.Count}/{trials} trials.");
		Assert.Equal(before, Capture(source.World));
		Assert.Empty(failures);

		(SearchFrame<BattleWorld, ActorRuntime> Frame, int Score) Best(
			IEnumerable<SearchFrame<BattleWorld, ActorRuntime>> frames) =>
			frames.Select(frame => (Frame: frame,
					Score: EnemySearchInput.ScoreHeuristic(frame, source, actorId, source.Actions.Count)))
				.OrderByDescending(candidate => candidate.Score)
				.First();
	}

	[Theory]
	[InlineData(0)]
	[InlineData(4)]
	public void PagedPathsReplayToTheirFramesAcrossPageBoundaries(int workers)
	{
		var source = CreateSource(ap: 3);
		Assert.True(source.TryEnqueue(new MoveStepAction("player")));
		BattleDef[] definitions = [.. Capabilities.Movement, LightningCannonDef.Instance];
		var bridge = SearchBridge.Run(source, "player", definitions,
			new SearchInput<BattleWorld, ActorRuntime>(BattleSearchVisit.ForCapabilities, Movement), workers: workers);
		Assert.True(bridge.Count > 256);
		for (var index = 0; index < bridge.Count; index++)
		{
			var path = bridge.Frames[index].Path;
			var full = bridge.FrameAt(index);
			Assert.Equal(source.Actions.Concat(path), full.Actions);
			Assert.Equal(path.Length, full.Depth);
			var replay = source.Fork();
			foreach (var action in path)
				Assert.True(replay.TryEnqueue(action));
			Assert.Equal(Capture(replay.World), Capture(full.World));
		}
	}

	[Theory]
	[InlineData(2, false)]
	[InlineData(4, false)]
	[InlineData(4, true)]
	public void GoroutineWorkersReturnReplayablePathsAndPreserveVisitCoverage(int workers, bool mixed)
	{
		var source = CreateSource(ap: 2, heat: 90);
		var before = Capture(source.World);
		BattleDef[] definitions = mixed
			? [.. Capabilities.Movement, LightningCannonDef.Instance]
			: [.. Capabilities.Movement];
		var input = new SearchInput<BattleWorld, ActorRuntime>(
			mixed ? BattleSearchVisit.ForCapabilities : BattleSearchVisit.ForMovePreview,
			mixed ? Movement : Translations(source.Actions.Count));
		var bridge = SearchBridge.Run(source, "player", definitions, input, workers: workers);
		Assert.Equal(workers, bridge.WorkersUsed);

		var expectedVisits = new HashSet<object>();
		var expectedWorlds = new HashSet<string>();
		var expectedBudgets = new Dictionary<object, List<int[]>>();
		foreach (var frame in SearchBridge.Enumerate(
			source, "player", definitions, input, TestContext.Current.CancellationToken))
		{
			var visitState = VisitOf(frame, mixed);
			expectedVisits.Add(visitState.State);
			AddBudget(expectedBudgets, visitState);
			expectedWorlds.Add(Capture(frame.World));
		}

		var actualVisits = new HashSet<object>();
		var actualWorlds = new HashSet<string>();
		var actualBudgets = new Dictionary<object, List<int[]>>();
		var seenRemaining = false;
		for (var index = 0; index < bridge.Count; index++)
		{
			var frame = bridge.FrameAt(index);
			seenRemaining |= frame.Phase == SearchFramePhase.Remaining;
			if (seenRemaining)
				Assert.Equal(SearchFramePhase.Remaining, frame.Phase);
			var replay = source.Fork();
			foreach (var action in frame.Actions)
				Assert.True(replay.TryEnqueue(action));
			var world = Capture(frame.World);
			Assert.Equal(Capture(replay.World), world);
			var visitState = VisitOf(frame, mixed);
			actualVisits.Add(visitState.State);
			AddBudget(actualBudgets, visitState);
			actualWorlds.Add(world);
		}
		Assert.True(expectedVisits.SetEquals(actualVisits),
			$"Visit coverage differs: missing={expectedVisits.Except(actualVisits).Count()}, "
			+ $"additional={actualVisits.Except(expectedVisits).Count()}.");
		foreach (var key in expectedVisits)
			Assert.Equal(
				ActionSearchExhaustive.NonDominatedBudgets(expectedBudgets[key])
					.Select(budget => string.Join(',', budget)).Order().ToArray(),
				ActionSearchExhaustive.NonDominatedBudgets(actualBudgets[key])
					.Select(budget => string.Join(',', budget)).Order().ToArray());
		output.WriteLine(
			$"workers={bridge.WorkersUsed}, frames={bridge.Count}, visit keys={actualVisits.Count}; "
			+ $"complete-world differences vs serial Go: "
			+ $"missing={expectedWorlds.Except(actualWorlds).Count()}, "
			+ $"additional={actualWorlds.Except(expectedWorlds).Count()}.");
		Assert.Equal(before, Capture(source.World));
	}

	private static void AddBudget(Dictionary<object, List<int[]>> budgets, SearchVisitState visit)
	{
		if (!budgets.TryGetValue(visit.State, out var frontier))
			budgets[visit.State] = frontier = [];
		frontier.Add(visit.Budget);
	}

	private static SearchVisitState VisitOf(SearchFrame<BattleWorld, ActorRuntime> frame, bool capabilities)
	{
		var simulation = new Simulation<BattleWorld, ActorRuntime>(frame.World, frame.Runtimes);
		simulation.Begin(frame.World.Timeline.Clock.Current, 0);
		var visit = capabilities
			? BattleSearchVisit.ForCapabilities(simulation, "player")
			: BattleSearchVisit.ForMovePreview(simulation, "player");
		visit.Budget[2] = -frame.Actions.Count(action => action.ActorId == "player");
		return visit;
	}

	private int AssertMatches(
		Simulation<BattleWorld, ActorRuntime> source,
		IReadOnlyList<BattleDef> definitions,
		Func<Simulation<BattleWorld, ActorRuntime>, string, SearchVisitState> visit,
		Func<IReadOnlyList<IAction>, bool>? priority,
		int pruneDepth = -1,
		int stopAfter = 0,
		Action<SearchFrame<BattleWorld, ActorRuntime>>? inspect = null,
		Func<Simulation<BattleWorld, ActorRuntime>, string, IAction, bool>? shouldExplore = null)
	{
		var before = Capture(source.World);
		var actionsBefore = source.Actions.ToArray();
		var input = new SearchInput<BattleWorld, ActorRuntime>(visit, priority, shouldExplore);
		var emitted = 0;
		var bridge = SearchBridge.Run(source, "player", definitions, input, frame =>
		{
			emitted++;
			if (stopAfter != 0 && emitted == stopAfter)
				return BridgeDecision.Stop;
			return frame.Depth == pruneDepth ? BridgeDecision.PruneChildren : BridgeDecision.Continue;
		});
		var count = 0;
		foreach (var expected in SearchBridge.Enumerate(
			source, "player", definitions, input, TestContext.Current.CancellationToken))
		{
			Assert.True(count < bridge.Count, $"Go omitted frame {count}.");
			var actual = bridge.FrameAt(count++);
			Assert.Equal(WithoutSpawnIds(string.Join(" -> ", expected.Actions)),
				WithoutSpawnIds(string.Join(" -> ", actual.Actions)));
			Assert.Equal(expected.Depth, actual.Depth);
			Assert.Equal(expected.Phase, actual.Phase);
			Assert.Equal(WithoutSpawnIds(Capture(expected.World)), WithoutSpawnIds(Capture(actual.World)));
			inspect?.Invoke(actual);
			if (count == stopAfter)
				break;
			expected.PruneChildren = expected.Depth == pruneDepth;
		}
		Assert.Equal(count, bridge.Count);
		Assert.Equal(actionsBefore, source.Actions.ToArray());
		Assert.Equal(before, Capture(source.World));
		return count;
	}

	internal static Func<IReadOnlyList<IAction>, bool> Movement { get; } =
		actions => actions.All(action => action is MoveStepAction or HeadingTurnAction or RollAction);

	internal static Func<IReadOnlyList<IAction>, bool> Translations(int prefix) =>
		actions => actions.Skip(prefix).All(action => action is MoveStepAction);

	private static SearchInput<BattleWorld, ActorRuntime> MovePreviewInput { get; } =
		new(BattleSearchVisit.ForMovePreview);

	// Each discovery mints a random spawned-unit id, so independent searches name the same spawn differently.
	private static string WithoutSpawnIds(string text) =>
		System.Text.RegularExpressions.Regex.Replace(text, "void_bomb-[a-z]+(-[a-z]+)*", "void_bomb");

	private string Capture(BattleWorld world) =>
		JsonSerializer.Serialize(SaveDtoMapper.CaptureBattleWorld(world, _registry), _registry.Options);

	private static Simulation<BattleWorld, ActorRuntime> CreateSource(int ap = 1, int heat = 0, bool lethal = false)
	{
		var battle = BattleTestFixture.BeginSimulation(
			BattleTestFixture.Player(new Coord(5, 5, 5)),
			BattleTestFixture.Enemy(new Coord(5, 5, 8)));
		var actor = battle.Engine.World.StateOf("player");
		actor.CurrentActionPoints = ap;
		actor.CurrentManeuverPoints = 1;
		actor.Heat = heat;
		var target = battle.Engine.World.StateOf("enemy");
		target.CurrentHullPoints = lethal ? 1 : 1_000;
		target.CurrentShieldPoints.Fill(lethal ? 0 : 5);
		return battle.Engine.CreateSimulation();
	}
}
