using GrimSpace.Core.Actions;
using GrimSpace.Core.Engine;

namespace GrimSpace.Tests.Engine;

public sealed class ActionBatchSinkTests
{
	[Fact]
	public async Task PublishThenWaitReturnsBatch()
	{
		var sink = new ActionBatchSink();
		var writer = sink.WriterFor("actor-a");
		var batch = new ActionBatch("actor-a", []);

		writer.Publish(batch);
		var result = await sink.WaitForBatchAsync("actor-a");

		Assert.True(result.IsSuccess);
		Assert.Same(batch, result.Batch);
	}

	[Fact]
	public async Task WaitThenPublishCompletesWaiter()
	{
		var sink = new ActionBatchSink();
		var writer = sink.WriterFor("actor-a");
		var waitTask = sink.WaitForBatchAsync("actor-a");
		Assert.False(waitTask.IsCompleted);

		var batch = new ActionBatch("actor-a", []);
		writer.Publish(batch);

		var result = await waitTask;
		Assert.True(result.IsSuccess);
		Assert.Same(batch, result.Batch);
	}

	[Fact]
	public void TryTakeBatchConsumesPendingBatch()
	{
		var sink = new ActionBatchSink();
		var writer = sink.WriterFor("actor-a");
		var batch = new ActionBatch("actor-a", []);
		writer.Publish(batch);

		Assert.True(sink.TryTakeBatch("actor-a", out var taken));
		Assert.Same(batch, taken);
		Assert.False(sink.TryTakeBatch("actor-a", out _));
	}

	[Fact]
	public async Task FailPropagatesThroughWait()
	{
		var sink = new ActionBatchSink();
		var writer = sink.WriterFor("actor-a");
		var failure = new InvalidOperationException("boom");
		writer.Fail(failure);

		var result = await sink.WaitForBatchAsync("actor-a");
		Assert.False(result.IsSuccess);
		Assert.Same(failure, result.Failure);
	}

	[Fact]
	public async Task ConcurrentPublishFromBackgroundThreadCompletesWaiter()
	{
		var sink = new ActionBatchSink();
		var writer = sink.WriterFor("actor-a");
		var waitTask = sink.WaitForBatchAsync("actor-a");
		var batch = new ActionBatch("actor-a", []);

		await Task.Run(() => writer.Publish(batch));

		var result = await waitTask;
		Assert.True(result.IsSuccess);
		Assert.Same(batch, result.Batch);
	}
}
