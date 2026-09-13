using GrimSpace.Core.Cache;

namespace GrimSpace.Tests.Caching;

public sealed class TickGenerationCacheTests
{
	[Fact]
	public void DuplicatePendingRequestsShareGeneration()
	{
		using var gate = new ManualResetEventSlim();
		var preparations = 0;
		using var cache = new TickGenerationCache<string, int, int, string>(
			capacity: 4,
			maxPending: 4,
			retentionTicks: 1,
			(input, cancellationToken) =>
			{
				Interlocked.Increment(ref preparations);
				gate.Wait(cancellationToken);
				return input * 2;
			},
			prepared => prepared.ToString());

		Assert.False(cache.Request("shape", () => 3, tick: 4, out _));
		Assert.False(cache.Request("shape", () => 99, tick: 4, out _));
		Assert.Equal(1, cache.PendingCount);
		Assert.True(SpinWait.SpinUntil(
			() => Volatile.Read(ref preparations) == 1,
			TimeSpan.FromSeconds(1)));

		gate.Set();
		Assert.True(SpinWait.SpinUntil(
			() => cache.CompletedPendingCount == 1,
			TimeSpan.FromSeconds(1)));
		Assert.Equal(1, cache.Pump(tick: 4).FinalizedCount);
		Assert.True(cache.Request("shape", () => 0, tick: 4, out var resource));
		Assert.Equal("6", resource);
	}

	[Fact]
	public void FinalizationRunsOnPumpingThread()
	{
		var pumpingThread = Environment.CurrentManagedThreadId;
		var preparationThread = pumpingThread;
		var finalizationThread = 0;
		using var cache = new TickGenerationCache<string, int, int, int>(
			capacity: 4,
			maxPending: 4,
			retentionTicks: 1,
			(input, _) =>
			{
				preparationThread = Environment.CurrentManagedThreadId;
				return input;
			},
			prepared =>
			{
				finalizationThread = Environment.CurrentManagedThreadId;
				return prepared;
			});

		Assert.False(cache.Request("shape", () => 3, tick: 1, out _));
		Assert.True(SpinWait.SpinUntil(
			() => cache.CompletedPendingCount == 1,
			TimeSpan.FromSeconds(1)));
		cache.Pump(tick: 1);

		Assert.NotEqual(pumpingThread, preparationThread);
		Assert.Equal(pumpingThread, finalizationThread);
	}

	[Fact]
	public void ResourcesExpireByTickNotElapsedTime()
	{
		using var cache = ImmediateCache(capacity: 4, retentionTicks: 1);
		Add(cache, "shape", tick: 5);

		Thread.Sleep(10);
		cache.AdvanceTick(6);
		Assert.Equal(1, cache.ResourceCount);
		cache.AdvanceTick(7);

		Assert.Equal(0, cache.ResourceCount);
	}

	[Fact]
	public void CapacityEvictsLeastRecentlyUsedResource()
	{
		using var cache = ImmediateCache(capacity: 2, retentionTicks: 10);
		Add(cache, "a", tick: 1);
		Add(cache, "b", tick: 1);
		Assert.True(cache.Request("a", () => 0, tick: 1, out _));
		Add(cache, "c", tick: 1);

		Assert.True(cache.Request("a", () => 0, tick: 1, out _));
		Assert.False(cache.Request("b", () => 2, tick: 1, out _));
		Assert.True(cache.Request("c", () => 0, tick: 1, out _));
	}

	[Fact]
	public void PreparationFailureIsSurfacedAndCanBeRetried()
	{
		var attempts = 0;
		using var cache = new TickGenerationCache<string, int, int, int>(
			capacity: 1,
			maxPending: 1,
			retentionTicks: 1,
			(input, _) => Interlocked.Increment(ref attempts) == 1
				? throw new InvalidOperationException("generation failed")
				: input,
			prepared => prepared);

		Assert.False(cache.Request("shape", () => 7, tick: 1, out _));
		Assert.True(SpinWait.SpinUntil(
			() => cache.CompletedPendingCount == 1,
			TimeSpan.FromSeconds(1)));
		var failed = cache.Pump(tick: 1);
		var error = Assert.Single(failed.Failures);
		Assert.Contains("generation failed", error.Error.ToString());

		Assert.False(cache.Request("shape", () => 7, tick: 1, out _));
		Assert.Equal(0, cache.PendingCount);
		cache.AdvanceTick(2);
		Assert.False(cache.Request("shape", () => 7, tick: 2, out _));
		Assert.True(SpinWait.SpinUntil(
			() => cache.CompletedPendingCount == 1,
			TimeSpan.FromSeconds(1)));
		Assert.Equal(1, cache.Pump(tick: 2).FinalizedCount);
		Assert.True(cache.Request("shape", () => 0, tick: 2, out var resource));
		Assert.Equal(7, resource);
	}

	[Fact]
	public void FailedPreparationDoesNotBlockHealthyCompletions()
	{
		using var cache = new TickGenerationCache<string, int, int, int>(
			capacity: 2,
			maxPending: 2,
			retentionTicks: 1,
			(input, _) => input < 0
				? throw new InvalidOperationException("invalid")
				: input,
			prepared => prepared);

		Assert.False(cache.Request("bad", () => -1, tick: 1, out _));
		Assert.False(cache.Request("good", () => 7, tick: 1, out _));
		Assert.True(SpinWait.SpinUntil(
			() => cache.CompletedPendingCount == 2,
			TimeSpan.FromSeconds(1)));

		var result = cache.Pump(tick: 1);

		Assert.Equal(1, result.FinalizedCount);
		Assert.Single(result.Failures);
		Assert.True(cache.Request("good", () => 0, tick: 1, out var resource));
		Assert.Equal(7, resource);
	}

	[Fact]
	public void FailedFinalizationDoesNotBlockHealthyCompletions()
	{
		using var cache = new TickGenerationCache<string, int, int, int>(
			capacity: 2,
			maxPending: 2,
			retentionTicks: 1,
			(input, _) => input,
			prepared => prepared < 0
				? throw new InvalidOperationException("invalid resource")
				: prepared);

		Assert.False(cache.Request("bad", () => -1, tick: 1, out _));
		Assert.False(cache.Request("good", () => 7, tick: 1, out _));
		Assert.True(SpinWait.SpinUntil(
			() => cache.CompletedPendingCount == 2,
			TimeSpan.FromSeconds(1)));

		var result = cache.Pump(tick: 1);

		Assert.Equal(1, result.FinalizedCount);
		Assert.Single(result.Failures);
		Assert.True(cache.Request("good", () => 0, tick: 1, out var resource));
		Assert.Equal(7, resource);
	}

	[Fact]
	public void PendingCapacityProvidesBackpressure()
	{
		using var gate = new ManualResetEventSlim();
		using var cache = new TickGenerationCache<string, int, int, int>(
			capacity: 4,
			maxPending: 1,
			retentionTicks: 1,
			(input, cancellationToken) =>
			{
				gate.Wait(cancellationToken);
				return input;
			},
			prepared => prepared);

		Assert.False(cache.Request("a", () => 1, tick: 1, out _));
		Assert.False(cache.Request("b", () => 2, tick: 1, out _));
		Assert.Equal(1, cache.PendingCount);

		gate.Set();
		Assert.True(SpinWait.SpinUntil(
			() => cache.CompletedPendingCount == 1,
			TimeSpan.FromSeconds(1)));
		cache.Pump(tick: 1);
		Assert.False(cache.Request("b", () => 2, tick: 1, out _));
		Assert.Equal(1, cache.PendingCount);
	}

	private static TickGenerationCache<string, int, int, int> ImmediateCache(
		int capacity,
		int retentionTicks) =>
		new(
			capacity,
			maxPending: capacity,
			retentionTicks,
			(input, _) => input,
			prepared => prepared);

	private static void Add(
		TickGenerationCache<string, int, int, int> cache,
		string key,
		int tick)
	{
		Assert.False(cache.Request(key, () => key[0], tick, out _));
		Assert.True(SpinWait.SpinUntil(
			() => cache.CompletedPendingCount > 0,
			TimeSpan.FromSeconds(1)));
		Assert.Equal(1, cache.Pump(tick).FinalizedCount);
	}
}
