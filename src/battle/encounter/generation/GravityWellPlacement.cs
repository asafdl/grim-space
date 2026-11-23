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

		var playerCenter = SingleCellHazardPlacement.Centroid(players);
		var opponentCenter = SingleCellHazardPlacement.Centroid(opponents);
		var count = context.Rng.Next(3);
		if (count == 0)
			return;
		var min = new Coord(
			System.Math.Clamp(System.Math.Min(playerCenter.X, opponentCenter.X), 1, context.GridSize - 2),
			System.Math.Clamp(System.Math.Min(playerCenter.Y, opponentCenter.Y), 1, context.GridSize - 2),
			System.Math.Clamp(System.Math.Min(playerCenter.Z, opponentCenter.Z), 1, context.GridSize - 2));
		var max = new Coord(
			System.Math.Clamp(System.Math.Max(playerCenter.X, opponentCenter.X), 1, context.GridSize - 2),
			System.Math.Clamp(System.Math.Max(playerCenter.Y, opponentCenter.Y), 1, context.GridSize - 2),
			System.Math.Clamp(System.Math.Max(playerCenter.Z, opponentCenter.Z), 1, context.GridSize - 2));
		var occupied = context.Spawns.Select(spawn => spawn.Position).ToHashSet();
		var candidates = new List<Coord>();
		for (var x = min.X; x <= max.X; x++)
		for (var y = min.Y; y <= max.Y; y++)
		for (var z = min.Z; z <= max.Z; z++)
			candidates.Add(new Coord(x, y, z));
		var shuffled = candidates.ToArray();
		context.Rng.Shuffle(shuffled);
		foreach (var center in shuffled)
		{
			if (count == 0)
				return;
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
			count--;
		}

		if (count > 0)
			throw new InvalidOperationException("Insufficient unoccupied volume between teams for gravity wells.");
	}
}
