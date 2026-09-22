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
			if (existing.SourceId == sourceId)
			{
				_overlays[key] = new Entry(role, sourceId);
				return;
			}

			throw new InvalidOperationException(
				$"Operator '{operatorName}' at facility '{facilityId}' already has a temporary role from '{existing.SourceId}'.");
		}

		_overlays[key] = new Entry(role, sourceId);
	}

	public void RevokeBySource(string sourceId)
	{
		ArgumentException.ThrowIfNullOrEmpty(sourceId);

		foreach (var key in _overlays.Keys.ToArray())
		{
			if (_overlays[key].SourceId == sourceId)
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
			clone._overlays[key] = entry;
		return clone;
	}

	private readonly record struct Entry(EFacilityOperatorRole Role, string SourceId);
}
