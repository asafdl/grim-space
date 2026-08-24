using GrimSpace.Math.Grid;
using GrimSpace.World.StarSystem.Poi;

namespace GrimSpace.World.StarSystem.Traffic;

internal static class DockLayout
{
	private const int DockOffset = 8;

	public static Dock CreateDock(
		PointOfInterest poi,
		PointOfInterest neighbour,
		PointOfInterest? star)
	{
		var (approachX, approachZ) = ApproachDirection(poi, neighbour, star);
		var position = Offset(poi.PlacedCenter, approachX, approachZ, poi.Radius + DockOffset);

		return new Dock(
			DockIdForPoi(poi.Id),
			poi.Id,
			position);
	}

	internal static string DockIdForPoi(string poiId) =>
		poiId.StartsWith("poi-", StringComparison.Ordinal)
			? $"dock-{poiId[4..]}"
			: throw new InvalidOperationException($"Cannot create dock id for POI '{poiId}'.");

	public static (double X, double Z) ApproachDirection(
		PointOfInterest poi,
		PointOfInterest neighbour,
		PointOfInterest? star)
	{
		var dx = neighbour.PlacedCenter.X - poi.PlacedCenter.X;
		var dz = neighbour.PlacedCenter.Z - poi.PlacedCenter.Z;
		if (dx == 0 && dz == 0 && star is not null)
		{
			dx = poi.PlacedCenter.X - star.PlacedCenter.X;
			dz = poi.PlacedCenter.Z - star.PlacedCenter.Z;
		}

		return Normalize(dx, dz);
	}

	public static (double X, double Z) Normalize(double x, double z)
	{
		var length = System.Math.Sqrt(x * x + z * z);
		if (length < 0.0001)
			return (0, 1);

		return (x / length, z / length);
	}

	public static Coord Offset(Coord origin, double dirX, double dirZ, double distance) =>
		new(
			(int)System.Math.Round(origin.X + dirX * distance),
			0,
			(int)System.Math.Round(origin.Z + dirZ * distance));
}
