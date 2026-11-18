using System.Diagnostics;
using GrimSpace.Battle;
using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Ai;
using GrimSpace.Battle.Encounter;
using GrimSpace.Battle.Units;
using GrimSpace.Units.Enums;

namespace GrimSpace.Tests.Ai;

[BattleTestSuite]
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
