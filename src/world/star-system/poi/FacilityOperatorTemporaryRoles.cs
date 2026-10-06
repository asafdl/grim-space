namespace GrimSpace.World.StarSystem.Poi;

/// <summary>
/// Run-time interaction roles for operators at one POI. Template <see cref="FacilityOperator.Role"/> values are unchanged.
/// </summary>
public sealed class FacilityOperatorTemporaryRoles
{
	private readonly Dictionary<(string FacilityId, string OperatorName), Entry> _overlays = new();

	public void Grant(
		string facilityId,
		string operatorName,
		EFacilityOperatorRole role,
		string sourceId)
	{
		ArgumentException.ThrowIfNullOrEmpty(facilityId);
		ArgumentException.ThrowIfNullOrEmpty(operatorName);
		ArgumentException.ThrowIfNullOrEmpty(sourceId);

		var key = (facilityId, operatorName);
		if (_overlays.TryGetValue(key, out var existing))
		{
			if (existing.Role == role)
			{
				existing.SourceIds.Add(sourceId);
				return;
			}

			if (existing.SourceIds.Count == 1 && existing.SourceIds.Contains(sourceId))
			{
				_overlays[key] = new Entry(role, [sourceId]);
				return;
			}

			throw new InvalidOperationException(
				$"Operator '{operatorName}' at facility '{facilityId}' already has temporary role " +
				$"'{existing.Role}' from '{string.Join(", ", existing.SourceIds.Order())}'.");
		}

		_overlays[key] = new Entry(role, [sourceId]);
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

	public FacilityOperatorTemporaryRoles CloneForFork()
	{
		var clone = new FacilityOperatorTemporaryRoles();
		foreach (var (key, entry) in _overlays)
			clone._overlays[key] = new Entry(
				entry.Role,
				new HashSet<string>(entry.SourceIds, StringComparer.Ordinal));
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

	private sealed record Entry(EFacilityOperatorRole Role, HashSet<string> SourceIds);
}
