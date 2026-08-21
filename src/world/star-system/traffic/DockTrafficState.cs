namespace GrimSpace.World.StarSystem.Traffic;

public sealed class DockTrafficState
{
	public DockTrafficState()
	{
	}

	private string? _arrivalOccupant;
	private string? _departureOccupant;
	private readonly LinkedList<string> _outsideQueue = new();
	private readonly HashSet<string> _queuedIds = new(StringComparer.Ordinal);

	public DockAdmissionResult TryAdmitArrival(string actorId)
	{
		ArgumentException.ThrowIfNullOrEmpty(actorId);

		if (_arrivalOccupant == actorId)
			return new DockAdmissionResult(true, false);

		if (_departureOccupant == actorId)
			throw new InvalidOperationException("Departure occupant cannot admit to arrival.");

		if (_arrivalOccupant is null)
		{
			_arrivalOccupant = actorId;
			RemoveFromQueue(actorId);
			return new DockAdmissionResult(true, false);
		}

		if (_queuedIds.Contains(actorId))
			return new DockAdmissionResult(false, false);

		_outsideQueue.AddLast(actorId);
		_queuedIds.Add(actorId);
		return new DockAdmissionResult(false, true);
	}

	public DockReleaseResult ReleaseArrival(string actorId)
	{
		if (_arrivalOccupant != actorId)
			return new DockReleaseResult(false, null);

		_arrivalOccupant = null;
		return new DockReleaseResult(true, DequeueHead());
	}

	public bool TryClaimDeparture(string actorId)
	{
		ArgumentException.ThrowIfNullOrEmpty(actorId);

		if (_departureOccupant == actorId)
			return true;

		if (_departureOccupant is not null)
			return false;

		if (_arrivalOccupant != actorId)
			return false;

		_departureOccupant = actorId;
		return true;
	}

	public DockReleaseResult ReleaseDeparture(string actorId)
	{
		if (_departureOccupant != actorId)
			return new DockReleaseResult(false, null);

		_departureOccupant = null;
		return new DockReleaseResult(true, null);
	}

	public void Validate()
	{
		if (_arrivalOccupant is not null && _queuedIds.Contains(_arrivalOccupant))
			throw new InvalidOperationException("Arrival occupant is also queued outside.");

		if (_departureOccupant is not null && _queuedIds.Contains(_departureOccupant))
			throw new InvalidOperationException("Departure occupant is also queued outside.");
	}

	public DockTrafficState Fork() =>
		new(
			_arrivalOccupant,
			_departureOccupant,
			_outsideQueue,
			_queuedIds);

	private DockTrafficState(
		string? arrivalOccupant,
		string? departureOccupant,
		IEnumerable<string> outsideQueue,
		IEnumerable<string> queuedIds)
	{
		_arrivalOccupant = arrivalOccupant;
		_departureOccupant = departureOccupant;
		_outsideQueue = new LinkedList<string>(outsideQueue);
		_queuedIds = new HashSet<string>(queuedIds, StringComparer.Ordinal);
	}

	private string? DequeueHead()
	{
		if (_outsideQueue.Count == 0)
			return null;

		var head = _outsideQueue.First!.Value;
		_outsideQueue.RemoveFirst();
		_queuedIds.Remove(head);
		return head;
	}

	private void RemoveFromQueue(string actorId)
	{
		if (!_queuedIds.Contains(actorId))
			return;

		for (var node = _outsideQueue.First; node is not null; node = node.Next)
		{
			if (node.Value != actorId)
				continue;

			_outsideQueue.Remove(node);
			_queuedIds.Remove(actorId);
			return;
		}
	}
}
