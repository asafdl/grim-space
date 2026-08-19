using GrimSpace.Battle.Ids;
using GrimSpace.Battle.Spatial;
using GrimSpace.Battle.Abilities;
using GrimSpace.Math.Grid;
using BoundedGrid = GrimSpace.Math.Grid.Grid;

namespace GrimSpace.Battle.World;

public sealed class Hazard : NonUnit
{
	public required Coord Center { get; init; }
	public required bool Passable { get; init; }
	public required int Damage { get; init; }
	public required int MomentumLoss { get; init; }
	public required EHazardKind Kind { get; init; }

	public static Hazard MissileZone(
		string id,
		string actorId,
		Coord center,
		BodyFrame ownerFrame,
		BoundedGrid grid,
		int radius,
		int damage,
		int momentumLoss) =>
		new()
		{
			Id = id,
			ActorId = actorId,
			Center = center,
			Frame = ownerFrame with { Origin = center },
			Cells = new HashSet<Coord>(grid.EnumerateCube(center, radius)),
			Passable = true,
			Damage = damage,
			MomentumLoss = momentumLoss,
			Kind = EHazardKind.MissileZone,
		};

	public static Hazard FlakBurst(
		string id,
		string actorId,
		BodyFrame ownerFrame,
		IEnumerable<Coord> cells) =>
		new()
		{
			Id = id,
			ActorId = actorId,
			Center = ownerFrame.Origin,
			Frame = ownerFrame,
			Cells = new HashSet<Coord>(cells),
			Passable = true,
			Damage = CombatConfig.FlakDamage,
			MomentumLoss = CombatConfig.FlakMomentumLoss,
			Kind = EHazardKind.FlakBurst,
		};

	public static Hazard Asteroid(
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

		return new Hazard
		{
			Id = id,
			ActorId = BattleActorIds.Terrain,
			Center = origin,
			Frame = BodyFrame.WorldAligned(origin),
			Cells = occupied,
			Passable = false,
			Damage = 0,
			MomentumLoss = 0,
			Kind = EHazardKind.Asteroid,
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

	public Hazard Clone() =>
		new()
		{
			Id = Id,
			ActorId = ActorId,
			Center = Center,
			Frame = Frame,
			Cells = new HashSet<Coord>(Cells),
			Passable = Passable,
			Damage = Damage,
			MomentumLoss = MomentumLoss,
			Kind = Kind,
		};
}
