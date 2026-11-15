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
}
