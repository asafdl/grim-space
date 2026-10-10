namespace GrimSpace.World.StarSystem.Poi;

/// <summary>
/// Run-time interaction roles for operators at one POI. Template <see cref="FacilityOperator.Role"/> values are unchanged.
/// </summary>
public sealed class FacilityOperatorTemporaryRoles
{
	private readonly Dictionary<(string FacilityId, string OperatorName), Entry> _overlays = new();
	private readonly Dictionary<(string FacilityId, string OperatorName), int> _contractPlacementPauses = new();

	public void Grant(
		string facilityId,
		string operatorName,
		EFacilityOperatorRole role,
		string sourceId,
		int? acceptsSourcesUntilTick = null)
	{
		ArgumentException.ThrowIfNullOrEmpty(facilityId);
		ArgumentException.ThrowIfNullOrEmpty(operatorName);
		ArgumentException.ThrowIfNullOrEmpty(sourceId);
		if (acceptsSourcesUntilTick is { } untilTick)
			ArgumentOutOfRangeException.ThrowIfNegative(untilTick);

		var key = (facilityId, operatorName);
		if (_overlays.TryGetValue(key, out var existing))
		{
			if (existing.Role == role)
			{
				if (acceptsSourcesUntilTick is not null
					&& existing.AcceptsSourcesUntilTick != acceptsSourcesUntilTick)
				{
					throw new InvalidOperationException(
						$"Operator '{operatorName}' at facility '{facilityId}' already has source " +
						$"acceptance deadline '{existing.AcceptsSourcesUntilTick?.ToString() ?? "none"}'.");
				}

				existing.SourceIds.Add(sourceId);
				return;
			}

			if (existing.SourceIds.Count == 1 && existing.SourceIds.Contains(sourceId))
			{
				_overlays[key] = new Entry(role, [sourceId], acceptsSourcesUntilTick);
				return;
			}

			throw new InvalidOperationException(
				$"Operator '{operatorName}' at facility '{facilityId}' already has temporary role " +
				$"'{existing.Role}' from '{string.Join(", ", existing.SourceIds.Order())}'.");
		}

		_overlays[key] = new Entry(role, [sourceId], acceptsSourcesUntilTick);
	}

	public void RevokeBySource(string sourceId)
	{
		ArgumentException.ThrowIfNullOrEmpty(sourceId);

		foreach (var key in _overlays.Keys.ToArray())
		{
			var entry = _overlays[key];
			entry.SourceIds.Remove(sourceId);
			if (entry.SourceIds.Count == 0)
				_overlays.Remove(key);
		}
	}

	public IReadOnlyList<Assignment> Assignments(EFacilityOperatorRole role) =>
		_overlays
			.Where(pair => pair.Value.Role == role)
			.OrderBy(pair => pair.Key.FacilityId, StringComparer.Ordinal)
			.ThenBy(pair => pair.Key.OperatorName, StringComparer.Ordinal)
			.Select(pair => ToAssignment(pair.Key, pair.Value))
			.ToArray();

	public bool TryGetAssignment(
		string facilityId,
		string operatorName,
		out Assignment assignment)
	{
		if (_overlays.TryGetValue((facilityId, operatorName), out var entry))
		{
			assignment = ToAssignment((facilityId, operatorName), entry);
			return true;
		}

		assignment = null!;
		return false;
	}

	public bool TryGetRole(string facilityId, string operatorName, out EFacilityOperatorRole role)
	{
		if (_overlays.TryGetValue((facilityId, operatorName), out var entry))
		{
			role = entry.Role;
			return true;
		}

		role = default;
		return false;
	}

	public bool IsAcceptingSources(
		string facilityId,
		string operatorName,
		EFacilityOperatorRole role,
		int currentTick) =>
		_overlays.TryGetValue((facilityId, operatorName), out var entry)
		&& entry.Role == role
		&& (entry.AcceptsSourcesUntilTick is null
			|| currentTick < entry.AcceptsSourcesUntilTick);

	public void RenewSourceAcceptance(
		string facilityId,
		string operatorName,
		int acceptsSourcesUntilTick)
	{
		ArgumentOutOfRangeException.ThrowIfNegative(acceptsSourcesUntilTick);

		if (!_overlays.TryGetValue((facilityId, operatorName), out var entry))
			throw new InvalidOperationException(
				$"Operator '{operatorName}' at facility '{facilityId}' has no temporary role.");

		entry.AcceptsSourcesUntilTick = acceptsSourcesUntilTick;
	}

	public void PauseContractPlacement(
		string facilityId,
		string operatorName,
		int untilTick)
	{
		ArgumentException.ThrowIfNullOrEmpty(facilityId);
		ArgumentException.ThrowIfNullOrEmpty(operatorName);
		ArgumentOutOfRangeException.ThrowIfNegative(untilTick);

		var key = (facilityId, operatorName);
		if (!_contractPlacementPauses.TryGetValue(key, out var currentUntilTick)
			|| untilTick > currentUntilTick)
			_contractPlacementPauses[key] = untilTick;
	}

	public bool IsContractPlacementPaused(
		string facilityId,
		string operatorName,
		int currentTick) =>
		_contractPlacementPauses.TryGetValue((facilityId, operatorName), out var untilTick)
		&& currentTick < untilTick;

	public void PruneExpiredContractPlacementPauses(int currentTick)
	{
		foreach (var key in _contractPlacementPauses
			.Where(pair => pair.Value <= currentTick)
			.Select(pair => pair.Key)
			.ToArray())
		{
			_contractPlacementPauses.Remove(key);
		}
	}

	public void PruneSources(EFacilityOperatorRole role, IReadOnlySet<string> retainedSourceIds)
	{
		ArgumentNullException.ThrowIfNull(retainedSourceIds);

		foreach (var key in _overlays.Keys.ToArray())
		{
			var entry = _overlays[key];
			if (entry.Role != role)
				continue;

			entry.SourceIds.RemoveWhere(sourceId => !retainedSourceIds.Contains(sourceId));
			if (entry.SourceIds.Count == 0)
				_overlays.Remove(key);
		}
	}

	public void Clear(EFacilityOperatorRole role)
	{
		foreach (var key in _overlays
			.Where(pair => pair.Value.Role == role)
			.Select(pair => pair.Key)
			.ToArray())
		{
			_overlays.Remove(key);
		}
	}

	public FacilityOperatorTemporaryRoles CloneForFork()
	{
		var clone = new FacilityOperatorTemporaryRoles();
		foreach (var (key, entry) in _overlays)
			clone._overlays[key] = new Entry(
				entry.Role,
				new HashSet<string>(entry.SourceIds, StringComparer.Ordinal),
				entry.AcceptsSourcesUntilTick);
		foreach (var (key, untilTick) in _contractPlacementPauses)
			clone._contractPlacementPauses[key] = untilTick;
		return clone;
	}

	internal IReadOnlyList<(string FacilityId, string OperatorName, EFacilityOperatorRole Role, string SourceId)> Snapshot() =>
		_overlays
			.SelectMany(pair => pair.Value.SourceIds.Select(sourceId =>
				(pair.Key.FacilityId, pair.Key.OperatorName, pair.Value.Role, sourceId)))
			.ToArray();

	internal static FacilityOperatorTemporaryRoles FromSnapshot(
		IEnumerable<(string FacilityId, string OperatorName, EFacilityOperatorRole Role, string SourceId)> entries)
	{
		var roles = new FacilityOperatorTemporaryRoles();
		foreach (var entry in entries)
			roles.Grant(entry.FacilityId, entry.OperatorName, entry.Role, entry.SourceId);
		return roles;
	}

	private static Assignment ToAssignment(
		(string FacilityId, string OperatorName) key,
		Entry entry) =>
		new(
			key.FacilityId,
			key.OperatorName,
			entry.Role,
			entry.SourceIds.Order(StringComparer.Ordinal).ToArray(),
			entry.AcceptsSourcesUntilTick);

	public sealed record Assignment(
		string FacilityId,
		string OperatorName,
		EFacilityOperatorRole Role,
		IReadOnlyList<string> SourceIds,
		int? AcceptsSourcesUntilTick);

	private sealed class Entry(
		EFacilityOperatorRole role,
		HashSet<string> sourceIds,
		int? acceptsSourcesUntilTick)
	{
		public EFacilityOperatorRole Role { get; } = role;
		public HashSet<string> SourceIds { get; } = sourceIds;
		public int? AcceptsSourcesUntilTick { get; set; } = acceptsSourcesUntilTick;
	}
}
