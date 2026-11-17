using System.Collections.Frozen;
using GrimSpace.Battle.Ids;
using GrimSpace.Battle.Spatial;
using GrimSpace.Math.Grid;
using BoundedGrid = GrimSpace.Math.Grid.Grid;

namespace GrimSpace.Battle.NonUnits;

public readonly record struct GravityWellFace(
	Coord Cell,
	Coord Normal,
	Coord Heading);

public sealed class GravityWell : Hazard
{
	public required FrozenSet<Coord> AssistCells { get; init; }
	public required FrozenSet<GravityWellFace> Faces { get; init; }
	public required FrozenDictionary<Coord, FrozenSet<Coord>> AssistedDirectionsByCell { get; init; }

	public Coord CoreCell => Center;

	private static readonly (int Starboard, int Fore)[] RingOffsets =
		[(1, -1), (1, 0), (1, 1), (0, 1), (-1, 1), (-1, 0), (-1, -1), (0, -1)];

	public override IEnumerable<Coord> MovementBlockingCells => [Center];
	public override IEnumerable<Coord> AbilityBlockingCells => [Center];

	public static GravityWell Create(
		string id,
		Coord center,
		BodyFrame frame,
		BoundedGrid grid,
		IEnumerable<Coord>? expectedCells = null)
	{
		if (center != frame.Origin)
			throw new ArgumentException("Gravity well frame origin must match its center.", nameof(frame));
		if (!IsValidFrame(frame))
			throw new ArgumentException("Gravity well frame must be an orthonormal grid frame.", nameof(frame));

		var axes = new[] { frame.Starboard, frame.Dorsal, frame.Fore };
		var cells = new HashSet<Coord>();
		for (var x = -1; x <= 1; x++)
		for (var y = -1; y <= 1; y++)
		for (var z = -1; z <= 1; z++)
			cells.Add(center + axes[0] * x + axes[1] * y + axes[2] * z);

		// Each dorsal layer's 8 cells around the core form a loop (counter-clockwise seen from dorsal).
		// Cells assist their outgoing heading; corners also keep their incoming heading.
		var ring = RingOffsets.Select(offset => frame.Starboard * offset.Starboard + frame.Fore * offset.Fore).ToArray();
		var faces = new HashSet<GravityWellFace>();
		var assistedDirections = new Dictionary<Coord, FrozenSet<Coord>>();
		for (var layer = -1; layer <= 1; layer++)
		for (var i = 0; i < ring.Length; i++)
		{
			var offset = frame.Dorsal * layer + ring[i];
			var headings = new[]
			{
				ring[(i + 1) % ring.Length] - ring[i],
				ring[i] - ring[(i + ring.Length - 1) % ring.Length],
			}.ToFrozenSet();
			assistedDirections.Add(center + offset, headings);
			foreach (var heading in headings)
			foreach (var axis in axes)
			{
				var side = Coord.Dot(offset, axis);
				if (side != 0 && Coord.Dot(axis, heading) == 0)
					faces.Add(new GravityWellFace(center + offset, axis * side, heading));
			}
		}

		if (cells.Any(cell => !grid.IsInBounds(cell)))
			throw new ArgumentOutOfRangeException(nameof(center), "Gravity well must fit entirely inside the battle grid.");
		if (expectedCells is not null && !cells.SetEquals(expectedCells))
			throw new ArgumentException("Gravity well cells must match its full 3x3x3 volume.", nameof(expectedCells));

		return new GravityWell
		{
			Id = id,
			ActorId = BattleActorIds.Terrain,
			Center = center,
			Frame = frame,
			Cells = cells.ToFrozenSet(),
			AssistCells = assistedDirections.Keys.ToFrozenSet(),
			Faces = faces.ToFrozenSet(),
			AssistedDirectionsByCell = assistedDirections.ToFrozenDictionary(),
		};
	}

	private static bool IsValidFrame(BodyFrame frame) =>
		IsCardinalUnit(frame.Fore)
		&& IsCardinalUnit(frame.Dorsal)
		&& IsCardinalUnit(frame.Starboard)
		&& Coord.Dot(frame.Fore, frame.Dorsal) == 0
		&& Coord.Dot(frame.Fore, frame.Starboard) == 0
		&& Coord.Dot(frame.Dorsal, frame.Starboard) == 0
		&& Coord.Cross(frame.Dorsal, frame.Fore) == frame.Starboard;

	private static bool IsCardinalUnit(Coord direction) =>
		System.Math.Abs(direction.X) + System.Math.Abs(direction.Y) + System.Math.Abs(direction.Z) == 1;
}
