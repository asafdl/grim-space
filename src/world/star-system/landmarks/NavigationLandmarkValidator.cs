using GrimSpace.Math.Grid;
using GrimSpace.World.StarSystem.Poi;
using GrimSpace.World.StarSystem.Traffic;

namespace GrimSpace.World.StarSystem.Landmarks;

public static class NavigationLandmarkValidator
{
	public static void Validate(
		int width,
		int height,
		IReadOnlyList<NavigationLandmark> landmarks,
		IReadOnlyList<PointOfInterest> pois,
		IReadOnlyCollection<Dock> docks)
	{
		ArgumentNullException.ThrowIfNull(landmarks);
		ArgumentNullException.ThrowIfNull(pois);
		ArgumentNullException.ThrowIfNull(docks);

		var reservedIds = new HashSet<string>(StringComparer.Ordinal);
		foreach (var poi in pois)
			reservedIds.Add(poi.Id);
		foreach (var dock in docks)
			reservedIds.Add(dock.Id);

		var displayNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		foreach (var poi in pois)
			displayNames.Add(poi.DisplayName);

		var seenIds = new HashSet<string>(StringComparer.Ordinal);
		foreach (var landmark in landmarks)
		{
			if (string.IsNullOrEmpty(landmark.Id))
				throw new InvalidOperationException("Navigation landmark id is required.");
			if (string.IsNullOrEmpty(landmark.DisplayName))
				throw new InvalidOperationException($"Navigation landmark '{landmark.Id}' requires a display name.");
			if (landmark.Radius <= 0)
				throw new InvalidOperationException($"Navigation landmark '{landmark.Id}' requires a positive radius.");
			if (!Enum.IsDefined(landmark.Kind))
				throw new InvalidOperationException($"Navigation landmark '{landmark.Id}' has an undefined kind.");

			if (!seenIds.Add(landmark.Id))
				throw new InvalidOperationException($"Duplicate navigation landmark id '{landmark.Id}'.");
			if (reservedIds.Contains(landmark.Id))
				throw new InvalidOperationException($"Navigation landmark id '{landmark.Id}' collides with a world object id.");

			if (!displayNames.Add(landmark.DisplayName))
				throw new InvalidOperationException($"Duplicate landmark display name '{landmark.DisplayName}'.");

			if (!GridBounds.IsCircleWhollyInRectangle(landmark.Position, landmark.Radius, width, height))
				throw new InvalidOperationException(
					$"Navigation landmark '{landmark.Id}' extends outside map bounds.");
		}
	}
}
