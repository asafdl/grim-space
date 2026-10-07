using System.Collections.Frozen;
using GrimSpace.Battle.Ids;
using GrimSpace.Battle.Spatial;
using GrimSpace.Math.Grid;
using BoundedGrid = GrimSpace.Math.Grid.Grid;

namespace GrimSpace.Battle.NonUnits;

public sealed class Asteroid : NonUnit
{
	public required Coord Center { get; init; }

	public override bool Passable => false;
	public override bool BlocksAbilities => true;

	public static Asteroid Create(
		string id,
		Coord origin,
		BoundedGrid grid,
		IEnumerable<Coord> cells)
	{
		var occupied = cells.ToHashSet();
		if (occupied.Count == 0)
			throw new ArgumentException("An asteroid must occupy at least one cell.", nameof(cells));
		if (!occupied.Contains(origin))
			throw new ArgumentException("The asteroid origin must be one of its occupied cells.", nameof(origin));
		if (occupied.Any(cell => !grid.IsInBounds(cell)))
			throw new ArgumentOutOfRangeException(nameof(cells), "Asteroid cells must be inside the battle grid.");
		if (!IsFaceConnected(occupied))
			throw new ArgumentException("An asteroid's occupied cells must form one connected shape.", nameof(cells));

		return new Asteroid
		{
			Id = id,
			ActorId = BattleActorIds.Terrain,
			Center = origin,
			Frame = BodyFrame.WorldAligned(origin),
			Cells = occupied.ToFrozenSet(),
		};
	}

	private static bool IsFaceConnected(IReadOnlySet<Coord> cells)
	{
		var visited = new HashSet<Coord>();
		var pending = new Queue<Coord>();
		var first = cells.First();
		visited.Add(first);
		pending.Enqueue(first);

		while (pending.TryDequeue(out var cell))
		{
			foreach (var neighbor in FaceNeighbors(cell))
			{
				if (cells.Contains(neighbor) && visited.Add(neighbor))
					pending.Enqueue(neighbor);
			}
		}

		return visited.Count == cells.Count;
	}

	private static IEnumerable<Coord> FaceNeighbors(Coord cell)
	{
		yield return cell + new Coord(1, 0, 0);
		yield return cell + new Coord(-1, 0, 0);
		yield return cell + new Coord(0, 1, 0);
		yield return cell + new Coord(0, -1, 0);
		yield return cell + new Coord(0, 0, 1);
		yield return cell + new Coord(0, 0, -1);
	}

}
