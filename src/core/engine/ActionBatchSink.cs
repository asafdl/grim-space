using System.Threading.Channels;

namespace GrimSpace.Core.Engine;

public sealed class ActionBatchSink
{
	private readonly object _gate = new();
	private readonly Dictionary<string, ActorSlot> _slots = new(StringComparer.Ordinal);

	public IActionBatchWriter WriterFor(string actorId)
	{
		ArgumentException.ThrowIfNullOrEmpty(actorId);

		lock (_gate)
			return GetOrCreateSlot(actorId).Writer;
	}

	public async Task<ActionProductionResult> WaitForBatchAsync(
		string actorId,
		CancellationToken cancellationToken = default)
	{
		ArgumentException.ThrowIfNullOrEmpty(actorId);

		ActorSlot slot;
		lock (_gate)
			slot = GetOrCreateSlot(actorId);

		return await slot.Reader.ReadAsync(cancellationToken);
	}

	public bool TryTakeBatch(string actorId, out ActionBatch batch)
	{
		ArgumentException.ThrowIfNullOrEmpty(actorId);

		lock (_gate)
		{
			var slot = GetOrCreateSlot(actorId);
			if (!slot.Reader.TryRead(out var result))
			{
				batch = null!;
				return false;
			}

			if (result.IsSuccess)
			{
				batch = result.Batch!;
				return true;
			}

			slot.Writer.TryWrite(result);
			batch = null!;
			return false;
		}
	}

	private ActorSlot GetOrCreateSlot(string actorId)
	{
		if (!_slots.TryGetValue(actorId, out var slot))
		{
			slot = new ActorSlot(this, actorId);
			_slots[actorId] = slot;
		}

		return slot;
	}

	private void Publish(string actorId, ActionBatch batch)
	{
		lock (_gate)
			GetOrCreateSlot(actorId).Writer.TryWrite(ActionProductionResult.FromBatch(batch));
	}

	private void Fail(string actorId, Exception exception)
	{
		ArgumentNullException.ThrowIfNull(exception);

		lock (_gate)
			GetOrCreateSlot(actorId).Writer.TryWrite(ActionProductionResult.FromFailure(exception));
	}

	private sealed class ActorSlot
	{
		private readonly Channel<ActionProductionResult> _channel = Channel.CreateBounded<ActionProductionResult>(
			new BoundedChannelOptions(1)
			{
				FullMode = BoundedChannelFullMode.DropOldest,
				SingleReader = false,
				SingleWriter = false,
			});

		public ActorSlot(ActionBatchSink sink, string actorId) =>
			Writer = new SinkWriter(sink, actorId, _channel.Writer);

		public ChannelReader<ActionProductionResult> Reader => _channel.Reader;

		public SinkWriter Writer { get; }
	}

	private sealed class SinkWriter : IActionBatchWriter
	{
		private readonly ActionBatchSink _sink;
		private readonly string _actorId;
		private readonly ChannelWriter<ActionProductionResult> _writer;

		public SinkWriter(
			ActionBatchSink sink,
			string actorId,
			ChannelWriter<ActionProductionResult> writer)
		{
			_sink = sink;
			_actorId = actorId;
			_writer = writer;
		}

		public void Publish(ActionBatch batch)
		{
			ArgumentNullException.ThrowIfNull(batch);
			if (!string.Equals(batch.ActorId, _actorId, StringComparison.Ordinal))
				throw new InvalidOperationException("Action batch actor id does not match writer actor id.");

			_sink.Publish(_actorId, batch);
		}

		public void Fail(Exception exception) => _sink.Fail(_actorId, exception);

		internal void TryWrite(ActionProductionResult result) => _writer.TryWrite(result);
	}
}
