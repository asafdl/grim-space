namespace GrimSpace.World.StarSystem.Traffic;

public sealed class PoiTrafficRegistry
{
	private readonly HashSet<string> _segmentIds;
	private readonly Dictionary<string, string> _segmentOwners = new(StringComparer.Ordinal);
	private readonly Dictionary<string, LinkedList<string>> _waitQueues = new(StringComparer.Ordinal);
	private readonly Dictionary<string, HashSet<string>> _waitingActors = new(StringComparer.Ordinal);
	private readonly Dictionary<string, HashSet<string>> _actorSegments = new(StringComparer.Ordinal);

	public PoiTrafficRegistry(IEnumerable<string> segmentIds)
	{
		_segmentIds = new HashSet<string>(segmentIds, StringComparer.Ordinal);
		foreach (var segmentId in _segmentIds)
		{
			_waitQueues[segmentId] = new LinkedList<string>();
			_waitingActors[segmentId] = new HashSet<string>(StringComparer.Ordinal);
		}
	}

	public AcquireResult TryAcquire(string actorId, string segmentId)
	{
		ArgumentException.ThrowIfNullOrEmpty(actorId);
		ArgumentException.ThrowIfNullOrEmpty(segmentId);

		if (!_segmentIds.Contains(segmentId))
			throw new ArgumentException($"Unknown segment: {segmentId}", nameof(segmentId));

		if (OwnsSegment(actorId, segmentId))
			return new AcquireResult(true, false);

		if (_segmentOwners.TryGetValue(segmentId, out var owner) && owner != actorId)
		{
			AddToWaitQueue(actorId, segmentId);
			return new AcquireResult(false, true);
		}

		_segmentOwners[segmentId] = actorId;
		SegmentsFor(actorId).Add(segmentId);
		RemoveFromAllQueues(actorId);
		return new AcquireResult(true, false);
	}

	public SegmentReleaseResult Release(string actorId, string segmentId)
	{
		if (_segmentOwners.GetValueOrDefault(segmentId) != actorId)
			return new SegmentReleaseResult(false, null);

		_segmentOwners.Remove(segmentId);
		SegmentsFor(actorId).Remove(segmentId);
		if (SegmentsFor(actorId).Count == 0)
			_actorSegments.Remove(actorId);

		return new SegmentReleaseResult(true, DequeueWaiter(segmentId));
	}

	public void Cancel(string actorId)
	{
		if (_actorSegments.TryGetValue(actorId, out var segments))
		{
			foreach (var segment in segments)
				_segmentOwners.Remove(segment);
			_actorSegments.Remove(actorId);
		}

		RemoveFromAllQueues(actorId);
	}

	public void Validate()
	{
		var ownedSegmentCount = _actorSegments.Values.Sum(set => set.Count);
		if (_segmentOwners.Count != ownedSegmentCount)
			throw new InvalidOperationException("Segment owner count does not match actor segment count.");

		foreach (var (segmentId, owner) in _segmentOwners)
		{
			if (!OwnsSegment(owner, segmentId))
				throw new InvalidOperationException($"Segment owner mismatch for {segmentId}.");
		}

		foreach (var (actorId, segments) in _actorSegments)
		{
			if (segments.Count > 1)
				throw new InvalidOperationException($"Actor {actorId} owns multiple segments.");

			foreach (var segmentId in segments)
			{
				if (_segmentOwners.GetValueOrDefault(segmentId) != actorId)
					throw new InvalidOperationException($"Actor segment mismatch for {actorId}.");
			}
		}

		foreach (var waiting in _waitingActors.Values)
		{
			foreach (var waitingActor in waiting)
			{
				if (_actorSegments.ContainsKey(waitingActor))
					throw new InvalidOperationException($"Waiting actor {waitingActor} owns a segment.");
			}
		}
	}

	public PoiTrafficRegistry Fork()
	{
		var clone = new PoiTrafficRegistry(_segmentIds);
		foreach (var (segmentId, owner) in _segmentOwners)
			clone._segmentOwners[segmentId] = owner;

		foreach (var (actorId, segments) in _actorSegments)
			clone._actorSegments[actorId] = new HashSet<string>(segments, StringComparer.Ordinal);

		foreach (var (segmentId, queue) in _waitQueues)
			clone._waitQueues[segmentId] = new LinkedList<string>(queue);

		foreach (var (segmentId, waiting) in _waitingActors)
			clone._waitingActors[segmentId] = new HashSet<string>(waiting, StringComparer.Ordinal);

		return clone;
	}

	private HashSet<string> SegmentsFor(string actorId) =>
		_actorSegments.TryGetValue(actorId, out var segments)
			? segments
			: _actorSegments[actorId] = new HashSet<string>(StringComparer.Ordinal);

	private bool OwnsSegment(string actorId, string segmentId) =>
		_actorSegments.TryGetValue(actorId, out var segments) && segments.Contains(segmentId);

	private bool IsWaiting(string actorId) =>
		_waitingActors.Values.Any(waiting => waiting.Contains(actorId));

	private void AddToWaitQueue(string actorId, string segmentId)
	{
		var waiting = _waitingActors[segmentId];
		if (waiting.Contains(actorId))
			return;

		_waitQueues[segmentId].AddLast(actorId);
		waiting.Add(actorId);
	}

	private string? DequeueWaiter(string segmentId)
	{
		var queue = _waitQueues[segmentId];
		if (queue.Count == 0)
			return null;

		var head = queue.First!.Value;
		queue.RemoveFirst();
		_waitingActors[segmentId].Remove(head);
		return head;
	}

	private void RemoveFromAllQueues(string actorId)
	{
		foreach (var segmentId in _segmentIds)
		{
			var waiting = _waitingActors[segmentId];
			if (!waiting.Contains(actorId))
				continue;

			waiting.Remove(actorId);
			var queue = _waitQueues[segmentId];
			for (var node = queue.First; node is not null; node = node.Next)
			{
				if (node.Value != actorId)
					continue;

				queue.Remove(node);
				break;
			}
		}
	}
}
