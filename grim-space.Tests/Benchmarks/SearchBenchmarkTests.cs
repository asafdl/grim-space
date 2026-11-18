using System.Diagnostics;
using System.Globalization;
using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Ai;
using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.Units;
using GrimSpace.Battle.World;
using GrimSpace.Core.Dfs;
using GrimSpace.Core.Engine;
using GrimSpace.Math.Grid;
using GrimSpace.Tests.Dfs;
using BattleDef = GrimSpace.Core.Actions.IActionDef<GrimSpace.Core.Actions.IAction, GrimSpace.Battle.World.BattleWorld, GrimSpace.Battle.Runtime.ActorRuntime, GrimSpace.Core.Actions.IEffect<GrimSpace.Battle.World.BattleWorld, GrimSpace.Battle.Runtime.ActorRuntime>>;

namespace GrimSpace.Tests.Benchmarks;

[BattleTestSuite]
public sealed class SearchBenchmarkTests(ITestOutputHelper output)
{
	private const int Samples = 3;
	private const double MiB = 1024 * 1024;
	private sealed record RunResult(IReadOnlyList<BridgeFrame> Frames, int WorkersUsed);
	private readonly record struct Measurement(
		double Milliseconds, double GcPauseMs, long ManagedBytes, ulong GoBytes, int Frames, int WorkersUsed);

	[Theory]
	[InlineData(8, false)]
	[InlineData(8, true)]
	public void CompareSerialAndGoroutineSearch(int ap, bool mixed) => Compare(ap, mixed, [1, 4, 8]);

	// Serial Go runs once, untimed and concurrently with the warm-ups, as the coverage reference. Replay
	// validation covers each warm-up and the last timed sample.
	private void Compare(int ap, bool mixed, int[] workerCounts)
	{
		if (Environment.GetEnvironmentVariable("DOTNET_TieredCompilation") != "0")
			throw new InvalidOperationException("Run benchmarks with DOTNET_TieredCompilation=0.");
		var runtime = SearchBridge.RuntimeInfo();
		Assert.False(runtime.RaceEnabled, "Rebuild without GOFLAGS=-race before benchmarking.");
		var source = CreateSource(ap, mixed);
		var input = new SearchInput<BattleWorld, ActorRuntime>(
			mixed ? BattleSearchVisit.ForCapabilities : BattleSearchVisit.ForMovePreview,
			mixed ? SearchBridgeTests.Movement : SearchBridgeTests.Translations(source.Actions.Count));
		BattleDef[] definitions = mixed
			? [.. Capabilities.Movement, LightningCannonDef.Instance]
			: [.. Capabilities.Movement];
		var workers = workerCounts.Distinct().Order().ToArray();
		var measurements = workers.ToDictionary(count => count, _ => new List<Measurement>());
		var scenario = mixed ? "mixed" : "movement";
		output.WriteLine($"CONFIG scenario={scenario} ap={ap} logical_cpus={Environment.ProcessorCount} " +
			$"gomaxprocs={runtime.Parallelism} samples={Samples} tiered_compilation=0 race=false");

		RunResult Run(int count)
		{
			var bridge = SearchBridge.Run(source, "player", definitions, input, workers: count);
			return new RunResult(bridge.Frames, bridge.WorkersUsed);
		}

		var reference = Task.Run(() => Inspect(source, Run(0), input));
		var warmups = workers.Select(count => Inspect(source, Run(count), input)).ToList();
		var expected = reference.Result;
		foreach (var actual in warmups)
			Check(expected, actual);

		for (var sample = 0; sample < Samples; sample++)
		{
			for (var index = 0; index < workers.Length; index++)
			{
				var count = workers[(index + sample) % workers.Length];
				MeasureAndValidate(count, sample == Samples - 1);
			}
		}
		foreach (var count in workers)
		{
			var values = measurements[count];
			var times = values.Select(value => value.Milliseconds).Order().ToArray();
			var pauses = values.Select(value => value.GcPauseMs).Order().ToArray();
			var managed = values.Select(value => value.ManagedBytes / MiB).Order().ToArray();
			var native = values.Select(value => value.GoBytes / MiB).Order().ToArray();
			output.WriteLine(FormattableString.Invariant(
				$"RESULT scenario={scenario} ap={ap} backend=Go{count} ms={times[Samples / 2]:F2} min_ms={times[0]:F2} max_ms={times[^1]:F2} gc_pause_ms={pauses[Samples / 2]:F2} managed_mib={managed[Samples / 2]:F2} go_mib={native[Samples / 2]:F2} frames={values.Min(value => value.Frames)}-{values.Max(value => value.Frames)} workers_used={values.Min(value => value.WorkersUsed)}-{values.Max(value => value.WorkersUsed)} samples_ms=[{string.Join(",", values.Select(value => value.Milliseconds.ToString("F2", CultureInfo.InvariantCulture)))}]"));
		}

		void MeasureAndValidate(int count, bool reportCoverage)
		{
			GC.Collect();
			GC.WaitForPendingFinalizers();
			GC.Collect();
			SearchBridge.Native.Collect();
			var goBefore = SearchBridge.Native.Allocated();
			var managedBefore = GC.GetTotalAllocatedBytes(precise: true);
			var pauseBefore = GC.GetTotalPauseDuration();
			var started = Stopwatch.GetTimestamp();
			var result = Run(count);
			var elapsed = Stopwatch.GetElapsedTime(started).TotalMilliseconds;
			var managedBytes = GC.GetTotalAllocatedBytes(precise: true) - managedBefore;
			var pause = (GC.GetTotalPauseDuration() - pauseBefore).TotalMilliseconds;
			var goBytes = SearchBridge.Native.Allocated() - goBefore;
			GC.KeepAlive(result);
			measurements[count].Add(new Measurement(
				elapsed, pause, managedBytes, goBytes, result.Frames.Count, result.WorkersUsed));
			Assert.InRange(result.WorkersUsed, System.Math.Min(2, count), count);
			if (!reportCoverage)
				return;
			var actual = Inspect(source, result, input);
			Check(expected, actual);
			output.WriteLine($"COVERAGE scenario={scenario} ap={ap} backend=Go{count} " +
					$"keys={actual.Count} replayed_paths={result.Frames.Count}");
		}
	}

	// Replays every returned path (throwing on an illegal step) and rebuilds its visit frontier.
	private static Dictionary<object, List<int[]>> Inspect(
		Simulation<BattleWorld, ActorRuntime> source, RunResult result,
		SearchInput<BattleWorld, ActorRuntime> input)
	{
		Assert.NotEmpty(result.Frames);
		var cursor = source.Fork();
		var budgets = new Dictionary<object, List<int[]>>();
		foreach (var frame in result.Frames)
		{
			SearchBridge.MoveTo(cursor, source.Actions.Count, frame.Path);
			var state = input.VisitState(cursor, "player");
			if (!budgets.TryGetValue(state.State, out var frontier))
				budgets[state.State] = frontier = [];
			frontier.Add(state.Budget);
		}
		return budgets.ToDictionary(entry => entry.Key,
			entry => ActionSearchExhaustive.NonDominatedBudgets(entry.Value).ToList());
	}

	private static void Check(Dictionary<object, List<int[]>> expected, Dictionary<object, List<int[]>> actual)
	{
		Assert.Empty(expected.Keys.Except(actual.Keys));
		Assert.Empty(actual.Keys.Except(expected.Keys));
		foreach (var (key, frontier) in expected)
			Assert.Equal(frontier.Select(budget => string.Join(",", budget)).Order(),
				actual[key].Select(budget => string.Join(",", budget)).Order());
	}

	private static Simulation<BattleWorld, ActorRuntime> CreateSource(int ap, bool mixed)
	{
		var battle = BattleTestFixture.BeginSimulation(
			BattleTestFixture.Player(new Coord(12, 12, 12)),
			BattleTestFixture.Enemy(mixed ? new Coord(12, 12, 15) : Coord.Zero),
			new GrimSpace.Math.Grid.Grid(24, 24, 24),
			new HashSet<Coord> { Coord.Zero });
		var actor = battle.Engine.World.StateOf("player");
		actor.CurrentActionPoints = ap;
		actor.CurrentManeuverPoints = 3;
		var target = battle.Engine.World.StateOf("enemy");
		target.CurrentHullPoints = 1_000;
		target.CurrentShieldPoints.Fill(5);
		return battle.Engine.CreateSimulation();
	}
}
