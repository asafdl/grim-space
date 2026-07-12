using GrimSpace.Math.Grid;
using BoundedGrid = GrimSpace.Math.Grid.Grid;

namespace GrimSpace.Battle.Weapons;

public sealed class Hazard
{
	public required Coord Center { get; init; }
	public required HashSet<Coord> Cells { get; init; }
	public required int Damage { get; init; }
	public required int MomentumLoss { get; init; }

	public static Hazard MissileZone(Coord center, BoundedGrid grid, int radius, int damage, int momentumLoss) =>
		new()
		{
			Center = center,
			Cells = new HashSet<Coord>(grid.EnumerateCube(center, radius)),
			Damage = damage,
			MomentumLoss = momentumLoss,
		};
}
