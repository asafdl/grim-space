using System.Collections.Concurrent;
using System.Diagnostics;
using GrimSpace.Battle;
using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Ai;
using GrimSpace.Battle.Encounter;
using GrimSpace.Battle.Units;
using GrimSpace.Core.Log;
using GrimSpace.Core.Dfs;
using GrimSpace.Units.Enums;

namespace GrimSpace.Tests.Ai;

[CollectionDefinition(nameof(EnemySearchPerformanceCollection), DisableParallelization = true)]
public sealed class EnemySearchPerformanceCollection
{
}

[BattleTestSuite]
[Collection(nameof(EnemySearchPerformanceCollection))]
public sealed class EnemySearchPerformanceTests(ITestOutputHelper output)
{
	[Fact]
	public void DevGooper_DecisionStaysWithinFrameBudget()
	{
		const int seed = 2_122_565_999;
		var battle = BattleOrchestrator.FromEncounter(
			BattleEncounter.DevDefault(seed),
			gridSize: 64);
		var gooper = EnemyGooper(battle);
		var timer = Stopwatch.StartNew();
		using var deadline = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
		deadline.CancelAfter(TimeSpan.FromSeconds(5));
		var search = AiController.Search((BattleSimulation)battle.Engine.CreateSimulation(), gooper.State.Id, deadline.Token);
		output.WriteLine($"Go workers={search.WorkersUsed}, scored={search.EvaluatedFrames}, ms={timer.Elapsed.TotalMilliseconds:F1}");
		Assert.Single(search.Frames);
		Assert.InRange(search.EvaluatedFrames, 1, 30_000);
		Assert.True(timer.Elapsed < TimeSpan.FromSeconds(5), $"AI decision took {timer.Elapsed}.");
	}

	[Fact]
	public void PlainFourApAdvanceDoesNotEnumerateMillionsOfFrames()
	{
		var simulation = (BattleSimulation)BattleTestFixture.LightningCannonDuel(stepsBeyondReach: 6, actionPoints: 4);
		var timer = Stopwatch.StartNew();
		using var deadline = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
		deadline.CancelAfter(TimeSpan.FromSeconds(5));
		var search = AiController.Search(simulation, "enemy", deadline.Token);
		var winner = search.FrameAt(0);
		output.WriteLine($"Go workers={search.WorkersUsed}, scored={search.EvaluatedFrames}, ms={timer.Elapsed.TotalMilliseconds:F1}");

		Assert.Single(search.Frames);
		Assert.InRange(search.EvaluatedFrames, 1, 5_000);
		Assert.True(search.BoundPrunedFrames > 0);
		Assert.Equal(0, winner.World.StateOf("enemy").CurrentActionPoints);
		Assert.Equal(EnemySearchInput.FacingWeight + 4 * EnemySearchInput.ApproachWeight, search.BestScore);
		Assert.True(timer.Elapsed < TimeSpan.FromSeconds(5), $"AI decision took {timer.Elapsed}.");
	}

	[Fact]
	public void CanonicalSpawnStatesReduceFullySkilledFighterSearch()
	{
		const int seed = 1_018_876_669;
		var encounter = BattleEncounter.DevDefault(seed);
		var fighter = encounter.Spawns.Single(spawn => spawn.Ship.Id == $"fighter-enemy-dev-{seed}");
		var player = encounter.Spawns.First(spawn => spawn.Team == ETeam.Player);
		var offset = fighter.Position + fighter.Fore * 14 - player.Position;
		using var battle = BattleOrchestrator.FromEncounter(new BattleEncounter
		{
			Id = encounter.Id,
			Seed = seed,
			Objective = encounter.Objective,
			WorldHazards = encounter.WorldHazards,
			DevPilots = encounter.DevPilots,
			Spawns = encounter.Spawns.Select(spawn => new BattleSpawn
			{
				Ship = spawn.Ship, Pilot = spawn.Pilot, Team = spawn.Team, Faction = spawn.Faction,
				Position = spawn.Team == ETeam.Player ? spawn.Position + offset : spawn.Position,
				Fore = spawn.Fore, Dorsal = spawn.Dorsal, ExecutionAgent = spawn.ExecutionAgent,
			}).ToArray(),
		}, gridSize: 64);
		var simulation = (BattleSimulation)battle.Engine.CreateSimulation();
		var actorId = fighter.Ship.Id;
		var originalIds = simulation.World.UnitRegistry.Ids.ToHashSet(StringComparer.Ordinal);
		var visit = BattleSearchVisit.ForCapabilitySearch(simulation);
		var timer = Stopwatch.StartNew();
		var rawIds = SearchBridge.FindBest(simulation, actorId,
			AiController.SearchCapabilities(simulation.World.StateOf(actorId), simulation.RuntimeFor(actorId)),
			new((branch, id) =>
			{
				var state = visit(branch, id);
				return new SearchVisitState(
					(state.State, string.Join('|', branch.World.UnitRegistry.Ids.Where(id => !originalIds.Contains(id)))),
					state.Budget);
			}, ShouldExploreAction: EnemySearchInput.ShouldExploreAction),
			EnemySearchInput.CreateEvaluator(simulation, actorId),
			workers: 8, cancellationToken: TestContext.Current.CancellationToken, timeBudget: TimeSpan.FromSeconds(15));
		var rawMilliseconds = timer.Elapsed.TotalMilliseconds;
		timer.Restart();
		var canonical = AiController.Search(simulation, actorId, TestContext.Current.CancellationToken,
			timeBudget: TimeSpan.FromSeconds(15));

		output.WriteLine($"raw={rawIds.EvaluatedFrames}, canonical={canonical.EvaluatedFrames}, " +
			$"raw_ms={rawMilliseconds:F1}, canonical_ms={timer.Elapsed.TotalMilliseconds:F1}, best={canonical.BestScore}");
		Assert.False(rawIds.BudgetExhausted);
		Assert.False(canonical.BudgetExhausted);
		Assert.Equal(rawIds.BestScore, canonical.BestScore);
		Assert.True(canonical.EvaluatedFrames < rawIds.EvaluatedFrames * 2 / 3,
			$"Expected at least one third fewer evaluations: raw={rawIds.EvaluatedFrames}, canonical={canonical.EvaluatedFrames}.");
		var replay = simulation.Fork();
		foreach (var action in Assert.Single(canonical.Frames).Path)
			Assert.True(replay.TryEnqueue(action));
	}

	[Fact]
	public void OversizedSearchStopsAtTheDefaultTwoSecondBudget()
	{
		var simulation = (BattleSimulation)BattleTestFixture.LightningCannonDuel(stepsBeyondReach: 6, actionPoints: 100);
		using var deadline = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
		deadline.CancelAfter(TimeSpan.FromSeconds(4));
		var timer = Stopwatch.StartNew();
		var search = AiController.Search(simulation, "enemy", deadline.Token);

		output.WriteLine($"Default budget: ms={timer.Elapsed.TotalMilliseconds:F1}, scored={search.EvaluatedFrames}");
		Assert.True(search.BudgetExhausted);
		Assert.Single(search.Frames);
		Assert.InRange(timer.Elapsed, TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(3));
		var replay = simulation.Fork();
		foreach (var action in search.Frames[0].Path)
			Assert.True(replay.TryEnqueue(action));
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public void DecisionBudgetWarnsOnlyWhenExhausted(bool exhaustBudget)
	{
		const int seed = 1_018_876_669;
		using var battle = BattleOrchestrator.FromEncounter(BattleEncounter.DevDefault(seed), gridSize: 64);
		var actorId = $"fighter-enemy-dev-{seed}";
		var logs = new ConcurrentQueue<string>();
		using var scope = GameLog.BeginScope(logs.Enqueue);
		var simulation = (BattleSimulation)battle.Engine.CreateSimulation();
		var search = AiController.Search(simulation, actorId, TestContext.Current.CancellationToken,
			timeBudget: exhaustBudget ? TimeSpan.FromTicks(1) : TimeSpan.FromSeconds(5));

		Assert.Equal(exhaustBudget, search.BudgetExhausted);
		Assert.Single(search.Frames);
		var message = Assert.Single(logs.Where(message => message.Contains(actorId)));
		Assert.Equal(exhaustBudget, message.Contains("warning:"));
		if (exhaustBudget)
			Assert.Contains("budget=", message);
		Assert.Contains("scored=", message);
		Assert.Contains("best=", message);
		Assert.Contains("dfs=", message);
	}

	[Fact]
	public void IndustrialGooper_SearchOmitsRotationallyEquivalentRolls()
	{
		var battle = BattleOrchestrator.FromEncounter(BattleEncounter.DevDefault());
		var gooper = EnemyGooper(battle);

		Assert.DoesNotContain(
			AiController.SearchCapabilities(
				gooper.State,
				battle.Engine.ActorRuntimes.For(gooper.State.Id)),
			definition => definition is RollDef);
	}

	private static Unit EnemyGooper(BattleOrchestrator battle) =>
		UnitRegistry.For(battle.Engine.World)
			.All.Single(unit => unit.State.Type == EType.IndustrialGooper && unit.Team == ETeam.Enemy);
}
