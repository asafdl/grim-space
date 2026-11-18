using System.Diagnostics;
using GrimSpace.Battle;
using GrimSpace.Battle.Ai;
using GrimSpace.Battle.Encounter;
using GrimSpace.Battle.Units;
using GrimSpace.Units.Enums;

namespace GrimSpace.Tests.Ai;

[BattleTestSuite]
public sealed class TmpEnemyTurnTiming(ITestOutputHelper output)
{
	[Fact]
	public void Time()
	{
		var outPath = Environment.GetEnvironmentVariable("TIMING_OUT");
		foreach (var seed in new[] { 1073117565, 2_122_565_999 })
		{
			var battle = BattleOrchestrator.FromEncounter(BattleEncounter.DevDefault(seed), gridSize: 64);
			foreach (var enemy in UnitRegistry.For(battle.Engine.World).All.Where(u => u.Team == ETeam.Enemy).ToList())
			{
				var id = enemy.State.Id;
				var session = (BattleSimulation)battle.Engine.CreateSimulation();
				var sw = Stopwatch.StartNew();
				using var deadline = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
				deadline.CancelAfter(TimeSpan.FromSeconds(5));
				var search = AiController.Search(session, id, deadline.Token);
				var message = $"seed={seed} type={enemy.State.Type} ap={enemy.State.CurrentActionPoints} "
					+ $"mp={enemy.State.CurrentManeuverPoints} scored={search.EvaluatedFrames} "
					+ $"workers={search.WorkersUsed} ms={sw.ElapsedMilliseconds}";
				output.WriteLine(message);
				if (outPath is not null)
					File.AppendAllText(outPath, message + Environment.NewLine);
				Assert.True(sw.Elapsed < TimeSpan.FromSeconds(5), message);
			}
		}
	}
}
