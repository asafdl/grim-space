namespace GrimSpace.World.StarSystem.Landmarks;

public static class MapLandmarkQueries
{
	public static IReadOnlyList<MapLandmarkRef> All(StarMap map)
	{
		ArgumentNullException.ThrowIfNull(map);

		var refs = new List<MapLandmarkRef>(map.PointsOfInterest.Count + map.NavigationLandmarks.Count);
		foreach (var poi in map.PointsOfInterest)
		{
			refs.Add(new MapLandmarkRef(
				poi.Id,
				poi.DisplayName,
				poi.PlacedCenter,
				poi.Radius,
				EMapLandmarkSource.PointOfInterest));
		}

		foreach (var landmark in map.NavigationLandmarks)
		{
			refs.Add(new MapLandmarkRef(
				landmark.Id,
				landmark.DisplayName,
				landmark.Position,
				landmark.Radius,
				EMapLandmarkSource.NavigationLandmark));
		}

		return refs;
	}

	public static bool TryGet(StarMap map, string id, out MapLandmarkRef reference)
	{
		ArgumentNullException.ThrowIfNull(map);
		ArgumentException.ThrowIfNullOrEmpty(id);

		foreach (var poi in map.PointsOfInterest)
		{
			if (string.Equals(poi.Id, id, StringComparison.Ordinal))
			{
				reference = new MapLandmarkRef(
					poi.Id,
					poi.DisplayName,
					poi.PlacedCenter,
					poi.Radius,
					EMapLandmarkSource.PointOfInterest);
				return true;
			}
		}

		foreach (var landmark in map.NavigationLandmarks)
		{
			if (string.Equals(landmark.Id, id, StringComparison.Ordinal))
			{
				reference = landmark.ToRef();
				return true;
			}
		}

		reference = default!;
		return false;
	}

	public static string? GetDisplayName(StarMap map, string id) =>
		TryGet(map, id, out var reference) ? reference.DisplayName : null;

	private static MapLandmarkRef ToRef(this NavigationLandmark landmark) =>
		new(
			landmark.Id,
			landmark.DisplayName,
			landmark.Position,
			landmark.Radius,
			EMapLandmarkSource.NavigationLandmark);
}
