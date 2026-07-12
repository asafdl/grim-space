using GrimSpace.Battle.Grid;
using GrimSpace.Domain.Grid;
using BattleGrid = GrimSpace.Battle.Grid.Grid;

namespace GrimSpace.Battle.Combat;

public sealed class Hazard
{
	public required Coord Center { get; init; }
	public required HashSet<Coord> Cells { get; init; }
	public required int Damage { get; init; }
	public required int MomentumLoss { get; init; }

	public static Hazard MissileZone(Coord center, BattleGrid grid, int radius, int damage, int momentumLoss)
	{
		var cells = new HashSet<Coord>();

		for (var dx = -radius; dx <= radius; dx++)
		{
			for (var dy = -radius; dy <= radius; dy++)
			{
				for (var dz = -radius; dz <= radius; dz++)
				{
					var cell = center + new Coord(dx, dy, dz);
					if (grid.IsInBounds(cell))
						cells.Add(cell);
				}
			}
		}

		return new Hazard
		{
			Center = center,
			Cells = cells,
			Damage = damage,
			MomentumLoss = momentumLoss,
		};
	}
}
