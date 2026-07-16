using GrimSpace.Math.Grid;
using BoundedGrid = GrimSpace.Math.Grid.Grid;

namespace GrimSpace.Battle.Weapons;

public sealed class Hazard
{
	public const int AsteroidBlockPadding = 1;

	public required Coord Center { get; init; }
	public required HashSet<Coord> Cells { get; init; }
	public required EHazardOwner Owner { get; init; }
	public required bool Passable { get; init; }
	public required int Damage { get; init; }
	public required int MomentumLoss { get; init; }
	public int Radius { get; init; }
	public string? VisualId { get; init; }

	public static Hazard MissileZone(Coord center, BoundedGrid grid, int radius, int damage, int momentumLoss) =>
		new()
		{
			Center = center,
			Cells = new HashSet<Coord>(grid.EnumerateCube(center, radius)),
			Owner = EHazardOwner.Turn,
			Passable = true,
			Damage = damage,
			MomentumLoss = momentumLoss,
		};

	public static int BlockRadiusFor(int radius) => radius + AsteroidBlockPadding;

	public static Hazard Asteroid(Coord center, BoundedGrid grid, int radius, string visualId) =>
		new()
		{
			Center = center,
			Cells = new HashSet<Coord>(grid.EnumerateCube(center, BlockRadiusFor(radius))),
			Owner = EHazardOwner.Board,
			Passable = false,
			Damage = 0,
			MomentumLoss = 0,
			Radius = radius,
			VisualId = visualId,
		};
}
