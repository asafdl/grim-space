using GrimSpace.Battle.Spatial;
using GrimSpace.Math.Grid;
using GrimSpace.Units.Enums;

namespace GrimSpace.Battle.Encounter.Generation;

public sealed class GravityWellPlacement : IHazardPlacementPass
{
	private const int ApproachRadius = 9;
	private const int MinCenterDistanceSquared = 36;
	private const int Count = 4;

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
		var min = new Coord(
			System.Math.Clamp(System.Math.Min(playerCenter.X, opponentCenter.X) - ApproachRadius, 1, context.GridSize - 2),
			System.Math.Clamp(System.Math.Min(playerCenter.Y, opponentCenter.Y) - ApproachRadius, 1, context.GridSize - 2),
			System.Math.Clamp(System.Math.Min(playerCenter.Z, opponentCenter.Z) - ApproachRadius, 1, context.GridSize - 2));
		var max = new Coord(
			System.Math.Clamp(System.Math.Max(playerCenter.X, opponentCenter.X) + ApproachRadius, 1, context.GridSize - 2),
			System.Math.Clamp(System.Math.Max(playerCenter.Y, opponentCenter.Y) + ApproachRadius, 1, context.GridSize - 2),
			System.Math.Clamp(System.Math.Max(playerCenter.Z, opponentCenter.Z) + ApproachRadius, 1, context.GridSize - 2));
		var occupied = context.Spawns.Select(spawn => spawn.Position).ToHashSet();
		var candidates = new List<Coord>();
		var approach = opponentCenter - playerCenter;
		var approachLengthSquared = Coord.Dot(approach, approach);
		for (var x = min.X; x <= max.X; x++)
		for (var y = min.Y; y <= max.Y; y++)
		for (var z = min.Z; z <= max.Z; z++)
		{
			var center = new Coord(x, y, z);
			if (HazardPlacementContext.IsWithinApproach(center, playerCenter, opponentCenter, ApproachRadius))
				candidates.Add(center);
		}
		var shuffled = candidates.ToArray();
		context.Rng.Shuffle(shuffled);
		var placed = new List<Coord>();
		PlaceNear(playerCenter);
		PlaceNear(opponentCenter);
		var midpointHeight = (playerCenter.Y + opponentCenter.Y) / 2;
		PlaceAtHeight(midpointHeight + 3, above: true);
		PlaceAtHeight(midpointHeight - 3, above: false);
		foreach (var center in shuffled)
		{
			if (placed.Count == Count)
				break;
			TryPlace(center);
		}

		if (placed.Count != Count)
			throw new InvalidOperationException($"Insufficient unoccupied volume to place {Count} gravity wells.");

		void PlaceAtHeight(int height, bool above)
		{
			foreach (var center in shuffled)
			{
				if ((above ? center.Y >= height : center.Y <= height) && TryPlace(center))
					return;
			}
		}

		void PlaceNear(Coord anchor)
		{
			foreach (var center in shuffled)
			{
				var offset = center - playerCenter;
				var projection = Coord.Dot(offset, approach);
				var isFlank = approachLengthSquared > 0
					&& (long)Coord.Dot(offset, offset) * approachLengthSquared - (long)projection * projection
						>= 9L * approachLengthSquared;
				var distance = center - anchor;
				if (isFlank && Coord.Dot(distance, distance) <= 64 && TryPlace(center))
					return;
			}
		}

		bool TryPlace(Coord center)
		{
			if (placed.Count >= Count)
				return false;
			var cells = Coord.OffsetsInCube(1).Select(offset => center + offset).ToHashSet();
			if (cells.Overlaps(occupied) || cells.Overlaps(context.ClaimedCells)
				|| placed.Any(other => Coord.Dot(center - other, center - other) < MinCenterDistanceSquared))
				return false;

			context.Claim(new BattleHazardSpawn
			{
				Kind = EBattleHazardKind.GravityWell,
				Origin = center,
				Frame = BodyFrame.WorldAligned(center),
				Cells = cells,
			});
			placed.Add(center);
			return true;
		}
	}
}
