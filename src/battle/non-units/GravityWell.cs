using System.Collections.Frozen;
using GrimSpace.Battle.Ids;
using GrimSpace.Battle.Spatial;
using GrimSpace.Math.Grid;
using BoundedGrid = GrimSpace.Math.Grid.Grid;

namespace GrimSpace.Battle.NonUnits;

public readonly record struct GravityWellFace(
	Coord Cell,
	Coord Normal,
	Coord FirstHeading,
	Coord SecondHeading);

public sealed class GravityWell : Hazard
{
	public required FrozenSet<Coord> AssistCells { get; init; }
	public required FrozenSet<GravityWellFace> Faces { get; init; }
	public required FrozenDictionary<Coord, FrozenSet<Coord>> AssistedDirectionsByCell { get; init; }

	public Coord CoreCell => Center;

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
		var assistCells = new HashSet<Coord>();
		var faces = new HashSet<GravityWellFace>();
		var assistedDirections = new Dictionary<Coord, HashSet<Coord>>();

		for (var x = -1; x <= 1; x++)
		for (var y = -1; y <= 1; y++)
		for (var z = -1; z <= 1; z++)
		{
			var cell = center + axes[0] * x + axes[1] * y + axes[2] * z;
			cells.Add(cell);
			if (cell != center)
			{
				assistCells.Add(cell);
				assistedDirections.Add(cell, []);
			}

			var coordinates = new[] { x, y, z };
			var side = SideOf(x, y, z);
			for (var faceAxis = 0; faceAxis < axes.Length; faceAxis++)
			{
				if (System.Math.Abs(coordinates[faceAxis]) != 1)
					continue;

				var tangents = Enumerable.Range(0, axes.Length)
					.Where(axis => axis != faceAxis)
					.Select(axis => axes[axis] * side)
					.ToArray();
				faces.Add(new GravityWellFace(
					cell,
					axes[faceAxis] * coordinates[faceAxis],
					tangents[0],
					tangents[1]));
				assistedDirections[cell].UnionWith(tangents);
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
			AssistCells = assistCells.ToFrozenSet(),
			Faces = faces.ToFrozenSet(),
			AssistedDirectionsByCell = assistedDirections.ToFrozenDictionary(
				entry => entry.Key,
				entry => entry.Value.ToFrozenSet()),
		};
	}

	private static int SideOf(int x, int y, int z) =>
		System.Math.Sign(x != 0 ? x : z != 0 ? z : y);

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
