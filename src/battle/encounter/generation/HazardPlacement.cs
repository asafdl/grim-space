using GrimSpace.Math.Grid;

namespace GrimSpace.Battle.Encounter.Generation;

/// <summary>
/// Entry point for encounter hazards: runs tactical placement passes around the engagement,
/// then fills the arena with atmospheric asteroids that avoid every claimed cell.
/// </summary>
public static class HazardPlacement
{
	private const int AsteroidFieldMargin = 2;

	private static readonly IHazardPlacementPass[] Passes =
		[
			new GravityWellPlacement(),
			new SingleCellHazardPlacement(EBattleHazardKind.CryoPlume, requireComplete: false),
			new SingleCellHazardPlacement(EBattleHazardKind.IonStorm, requireComplete: true),
		];

	public static IReadOnlyList<BattleHazardSpawn> Generate(
		int seed,
		int gridSize,
		IReadOnlyList<BattleSpawn> spawns)
	{
		var context = new HazardPlacementContext
		{
			Rng = new Random(seed),
			GridSize = gridSize,
			Spawns = spawns,
		};
		foreach (var pass in Passes)
			pass.Place(context);

		var reserved = AsteroidFieldReservations.PlayerCorridors(spawns);
		reserved.UnionWith(context.ClaimedCells);
		var center = gridSize / 2;
		var asteroids = AsteroidFieldGenerator.Generate(new AsteroidFieldConfig
		{
			Seed = seed,
			GridSize = gridSize,
			UnitPositions = spawns.Select(spawn => spawn.Position).ToArray(),
			RegionCenter = new Coord(center, center, center),
			RegionHalfExtent = center - AsteroidFieldMargin,
			RegionMargin = AsteroidFieldMargin,
			ReservedCells = reserved,
		});

		return [.. context.Placed, .. asteroids];
	}
}
