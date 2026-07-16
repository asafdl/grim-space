using GrimSpace.Battle.Ids;
using GrimSpace.Math.Grid;
using BoundedGrid = GrimSpace.Math.Grid.Grid;

namespace GrimSpace.Battle.Board;

public sealed class Hazard : NonUnit
{
	public const int AsteroidBlockPadding = 1;

	public required Coord Center { get; init; }
	public required bool Passable { get; init; }
	public required int Damage { get; init; }
	public required int MomentumLoss { get; init; }
	public int Radius { get; init; }
	public string? VisualId { get; init; }

	public static Hazard MissileZone(
		string id,
		string ownerId,
		Coord center,
		BoundedGrid grid,
		int radius,
		int damage,
		int momentumLoss) =>
		new()
		{
			Id = id,
			OwnerId = ownerId,
			Center = center,
			Cells = new HashSet<Coord>(grid.EnumerateCube(center, radius)),
			Passable = true,
			Damage = damage,
			MomentumLoss = momentumLoss,
		};

	public static int BlockRadiusFor(int radius) => radius + AsteroidBlockPadding;

	public static Hazard Asteroid(
		string id,
		Coord center,
		BoundedGrid grid,
		int radius,
		string visualId) =>
		new()
		{
			Id = id,
			OwnerId = EntityIds.Board,
			Center = center,
			Cells = new HashSet<Coord>(grid.EnumerateCube(center, BlockRadiusFor(radius))),
			Passable = false,
			Damage = 0,
			MomentumLoss = 0,
			Radius = radius,
			VisualId = visualId,
		};

	public Hazard Clone() =>
		new()
		{
			Id = Id,
			OwnerId = OwnerId,
			Center = Center,
			Cells = new HashSet<Coord>(Cells),
			Passable = Passable,
			Damage = Damage,
			MomentumLoss = MomentumLoss,
			Radius = Radius,
			VisualId = VisualId,
		};
}
