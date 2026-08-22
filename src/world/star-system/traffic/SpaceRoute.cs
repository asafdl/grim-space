using GrimSpace.Math.Grid;

namespace GrimSpace.World.StarSystem.Traffic;

public sealed record SpaceRoute(
	string Id,
	string DockAId,
	string DockBId,
	IReadOnlyList<Coord> Centerline,
	int HalfWidth)
{
	public double Length { get; } = ComputeLength(Centerline);

	private static double ComputeLength(IReadOnlyList<Coord> centerline) =>
		centerline.Zip(centerline.Skip(1))
			.Sum(pair =>
			{
				var dx = pair.Second.X - pair.First.X;
				var dz = pair.Second.Z - pair.First.Z;
				return System.Math.Sqrt(dx * dx + dz * dz);
			});
}
