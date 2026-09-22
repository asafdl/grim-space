using GrimSpace.World.StarSystem;
using GrimSpace.World.StarSystem.Poi;

namespace GrimSpace.World.StarSystem.Presentation;

internal static class FacilityLookup
{
	public static Facility Get(StarMap map, string poiId, string facilityId)
	{
		var poi = map.PointsOfInterest.FirstOrDefault(candidate =>
			string.Equals(candidate.Id, poiId, StringComparison.Ordinal));
		if (poi is null)
			throw new InvalidOperationException($"Unknown POI '{poiId}'.");

		var facility = poi.Facilities.FirstOrDefault(candidate =>
			string.Equals(candidate.Id, facilityId, StringComparison.Ordinal));
		if (facility is null)
			throw new InvalidOperationException($"Unknown facility '{facilityId}' at POI '{poiId}'.");

		return facility;
	}
}
