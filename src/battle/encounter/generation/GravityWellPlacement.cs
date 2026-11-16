using GrimSpace.Battle.Spatial;
using GrimSpace.Math.Grid;
using GrimSpace.Units.Enums;

namespace GrimSpace.Battle.Encounter.Generation;

public sealed class GravityWellPlacement : IHazardPlacementPass
{
	public void Place(HazardPlacementContext context)
	{
		ArgumentOutOfRangeException.ThrowIfLessThan(context.GridSize, 3);
		var players = context.Spawns.Where(spawn => spawn.Team == ETeam.Player)
			.Select(spawn => spawn.Position).ToArray();
		var opponents = context.Spawns.Where(spawn => spawn.Team != ETeam.Player)
			.Select(spawn => spawn.Position).ToArray();
		if (players.Length == 0 || opponents.Length == 0)
			throw new InvalidOperationException("Gravity well placement requires both sides of the engagement.");

		var playerCenter = CryoPlumePlacement.Centroid(players);
		var opponentCenter = CryoPlumePlacement.Centroid(opponents);
		var middle = new Coord(
			System.Math.Clamp((playerCenter.X + opponentCenter.X) / 2, 1, context.GridSize - 2),
			System.Math.Clamp((playerCenter.Y + opponentCenter.Y) / 2, 1, context.GridSize - 2),
			System.Math.Clamp((playerCenter.Z + opponentCenter.Z) / 2, 1, context.GridSize - 2));
		var occupied = context.Spawns.Select(spawn => spawn.Position).ToHashSet();
		for (var radius = 0; radius < context.GridSize; radius++)
		for (var x = -radius; x <= radius; x++)
		for (var y = -radius; y <= radius; y++)
		for (var z = -radius; z <= radius; z++)
		{
			if (System.Math.Max(System.Math.Abs(x), System.Math.Max(System.Math.Abs(y), System.Math.Abs(z))) != radius)
				continue;
			var center = middle + new Coord(x, y, z);
			if (center.X < 1 || center.Y < 1 || center.Z < 1
				|| center.X >= context.GridSize - 1
				|| center.Y >= context.GridSize - 1
				|| center.Z >= context.GridSize - 1)
				continue;
			var cells = Coord.OffsetsInCube(1).Select(offset => center + offset).ToHashSet();
			if (cells.Overlaps(occupied) || cells.Overlaps(context.ClaimedCells))
				continue;

			context.Claim(new BattleHazardSpawn
			{
				Kind = EBattleHazardKind.GravityWell,
				Origin = center,
				Frame = BodyFrame.WorldAligned(center),
				Cells = cells,
			});
			return;
		}

		throw new InvalidOperationException("No unoccupied volume is available for a gravity well.");
	}
}
