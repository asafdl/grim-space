using GrimSpace.Core.Ids;
using GrimSpace.Math.Grid;

namespace GrimSpace.World.StarSystem.Traffic;

internal static class DockLayout
{
	private const int DockOffset = 8;

	public static Dock CreateDock(
		TypedIdGenerator ids,
		PointOfInterest poi,
		PointOfInterest station,
		PointOfInterest? star)
	{
		var (approachX, approachZ) = ApproachDirection(poi, station, star);
		var position = Offset(poi.Center, approachX, approachZ, poi.Radius + DockOffset);

		return new Dock(
			ids.NextId("dock"),
			poi.Id,
			position);
	}

	public static (double X, double Z) ApproachDirection(
		PointOfInterest poi,
		PointOfInterest station,
		PointOfInterest? star)
	{
		var dx = station.Center.X - poi.Center.X;
		var dz = station.Center.Z - poi.Center.Z;
		if (dx == 0 && dz == 0 && star is not null)
		{
			dx = poi.Center.X - star.Center.X;
			dz = poi.Center.Z - star.Center.Z;
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
