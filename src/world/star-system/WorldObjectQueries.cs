using GrimSpace.Math.Grid;
using GrimSpace.World.StarSystem.Landmarks;

namespace GrimSpace.World.StarSystem;

public static class WorldObjectQueries
{
	public static WorldObjectResolution ResolveFocusable(
		StarMap world,
		string objectId,
		Func<string, Coord> committedPositionOf,
		Func<string, bool>? isFleetVisible = null)
	{
		ArgumentNullException.ThrowIfNull(world);
		ArgumentException.ThrowIfNullOrWhiteSpace(objectId);
		ArgumentNullException.ThrowIfNull(committedPositionOf);

		var pois = world.PointsOfInterest.Where(poi => poi.Id == objectId).Take(2).ToArray();
		var landmarks = world.NavigationLandmarks.Where(landmark => landmark.Id == objectId).Take(2).ToArray();
		var docks = world.DocksById.Values.Where(dock => dock.Id == objectId).Take(2).ToArray();
		var hasUnit = world.FleetRegistry.TryGet(objectId, out _);
		var matches = pois.Length + landmarks.Length + docks.Length + (hasUnit ? 1 : 0);

		if (matches == 0)
			return new WorldObjectResolution.Missing();

		if (matches > 1)
			return new WorldObjectResolution.Ambiguous();

		if (pois.Length == 1)
		{
			var poi = pois[0];
			return poi.Center is { } center
				? new WorldObjectResolution.Found(center)
				: new WorldObjectResolution.NotFocusable();
		}

		if (landmarks.Length == 1)
			return new WorldObjectResolution.Found(landmarks[0].Position);

		if (docks.Length == 1)
			return new WorldObjectResolution.Found(docks[0].Position);

		if (isFleetVisible?.Invoke(objectId) == false)
			return new WorldObjectResolution.NotFocusable();

		return new WorldObjectResolution.Found(committedPositionOf(objectId));
	}
}

public abstract record WorldObjectResolution
{
	public sealed record Found(Coord Position) : WorldObjectResolution;

	public sealed record Missing : WorldObjectResolution;

	public sealed record Ambiguous : WorldObjectResolution;

	public sealed record NotFocusable : WorldObjectResolution;
}
