using GrimSpace.Math.Grid;

namespace GrimSpace.World.StarSystem.Traffic;

public sealed record LaneSegment(
	string Id,
	IReadOnlyList<Coord> Points)
{
	public Coord Start => Points[0];
	public Coord End => Points[^1];

	public double Length =>
		Points.Zip(Points.Skip(1))
			.Sum(pair =>
			{
				var dx = pair.Second.X - pair.First.X;
				var dz = pair.Second.Z - pair.First.Z;
				return System.Math.Sqrt(dx * dx + dz * dz);
			});
}
