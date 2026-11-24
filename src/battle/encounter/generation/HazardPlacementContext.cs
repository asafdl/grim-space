using GrimSpace.Math.Grid;

namespace GrimSpace.Battle.Encounter.Generation;

/// <summary>Shared state threaded through hazard placement passes for one encounter.</summary>
public sealed class HazardPlacementContext
{
	public required Random Rng { get; init; }
	public required int GridSize { get; init; }
	public required IReadOnlyList<BattleSpawn> Spawns { get; init; }
	public HashSet<Coord> ClaimedCells { get; } = [];
	public List<BattleHazardSpawn> Placed { get; } = [];

	public void Claim(BattleHazardSpawn hazard)
	{
		Placed.Add(hazard);
		ClaimedCells.UnionWith(hazard.Cells);
	}

	public static bool IsWithinApproach(Coord cell, Coord start, Coord end, int radius)
	{
		var direction = end - start;
		var offset = cell - start;
		var lengthSquared = Coord.Dot(direction, direction);
		var offsetSquared = Coord.Dot(offset, offset);
		if (lengthSquared == 0)
			return offsetSquared <= radius * radius;

		var projection = Coord.Dot(offset, direction);
		return projection >= 0 && projection <= lengthSquared
			&& (long)offsetSquared * lengthSquared - (long)projection * projection
				<= (long)radius * radius * lengthSquared;
	}
}
