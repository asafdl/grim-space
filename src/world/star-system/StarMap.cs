using GrimSpace.Core.Engine;
using GrimSpace.Core.Ids;
using GrimSpace.Math.Grid;

namespace GrimSpace.World.StarSystem;

public sealed class StarMap : IWorld<StarMap>
{
	public const int DevGridWidth = 32;
	public const int DevGridHeight = 32;

	public int Seed { get; }
	public int Width { get; }
	public int Height { get; }
	public IReadOnlyList<PointOfInterest> PointsOfInterest { get; }
	public Timeline Timeline { get; }

	private StarMap(
		int seed,
		int width,
		int height,
		IReadOnlyList<PointOfInterest> pointsOfInterest,
		Timeline timeline)
	{
		Seed = seed;
		Width = width;
		Height = height;
		PointsOfInterest = pointsOfInterest;
		Timeline = timeline;
	}

	public bool IsInBounds(Coord cell) =>
		cell.Y == 0 && cell.X >= 0 && cell.X < Width && cell.Z >= 0 && cell.Z < Height;

	public StarMap Fork() =>
		new(Seed, Width, Height, PointsOfInterest, Timeline.Clone());

	public static StarMap CreateDevDefault(int seed = 0)
	{
		var ids = new TypedIdGenerator();
		var pois = new PointOfInterest[]
		{
			new()
			{
				Id = ids.NextId("star"),
				Cells = Rect(14, 14, 4, 4),
			},
			new()
			{
				Id = ids.NextId("planet"),
				Cells = Rect(4, 6, 2, 2),
			},
			new()
			{
				Id = ids.NextId("planet"),
				Cells = Rect(22, 20, 2, 2),
			},
			new()
			{
				Id = ids.NextId("station"),
				Cells = Rect(24, 5, 1, 1),
			},
		};

		return new StarMap(seed, DevGridWidth, DevGridHeight, pois, new Timeline());
	}

	private static HashSet<Coord> Rect(int x, int z, int width, int depth)
	{
		var cells = new HashSet<Coord>();
		for (var dx = 0; dx < width; dx++)
		{
			for (var dz = 0; dz < depth; dz++)
				cells.Add(new Coord(x + dx, 0, z + dz));
		}

		return cells;
	}
}
