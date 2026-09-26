using GrimSpace.Battle.Encounter;
using GrimSpace.Math.Grid;

namespace GrimSpace.Battle.Encounter.Generation;

public static class AsteroidFieldGenerator
{
	public static IReadOnlyList<BattleHazardSpawn> Generate(AsteroidFieldConfig config)
	{
		var rng = new Random(config.Seed);
		var placed = new List<BattleHazardSpawn>();
		var maxAttempts = config.TargetCount * 100;

		for (var attempt = 0; attempt < maxAttempts && placed.Count < config.TargetCount; attempt++)
		{
			var localCells = CreateVolume(PickSize(rng));
			var origin = PickOrigin(config, rng);
			var cells = localCells.Select(cell => cell + origin).ToHashSet();

			if (!FitsField(cells, config))
				continue;

			if (!IsClearOfUnits(cells, config))
				continue;

			if (!IsClearOfAsteroids(cells, placed, config.AsteroidGap))
				continue;

			placed.Add(new BattleHazardSpawn { Origin = origin, Cells = cells });
		}

		return placed;
	}

	private static int PickSize(Random rng) =>
		rng.Next(100) switch
		{
			< 20 => 1,
			< 60 => 2,
			< 92 => 3,
			_ => 4,
		};

	private static HashSet<Coord> CreateVolume(int size)
	{
		var cells = new HashSet<Coord>();
		var min = -(size / 2);
		for (var x = min; x < min + size; x++)
		for (var y = min; y < min + size; y++)
		for (var z = min; z < min + size; z++)
			cells.Add(new Coord(x, y, z));
		return cells;
	}

	private static Coord PickOrigin(AsteroidFieldConfig config, Random rng) =>
		config.RegionCenter + new Coord(
			rng.Next(-config.RegionHalfExtent, config.RegionHalfExtent + 1),
			rng.Next(-config.RegionHalfExtent, config.RegionHalfExtent + 1),
			rng.Next(-config.RegionHalfExtent, config.RegionHalfExtent + 1));

	private static bool FitsField(IReadOnlySet<Coord> cells, AsteroidFieldConfig config)
	{
		var minGrid = config.RegionMargin;
		var maxGrid = config.GridSize - config.RegionMargin - 1;
		var regionMin = config.RegionCenter - new Coord(config.RegionHalfExtent, config.RegionHalfExtent, config.RegionHalfExtent);
		var regionMax = config.RegionCenter + new Coord(config.RegionHalfExtent, config.RegionHalfExtent, config.RegionHalfExtent);

		return cells.All(cell =>
			cell.X >= minGrid && cell.X <= maxGrid
			&& cell.Y >= minGrid && cell.Y <= maxGrid
			&& cell.Z >= minGrid && cell.Z <= maxGrid
			&& cell.X >= regionMin.X && cell.X <= regionMax.X
			&& cell.Y >= regionMin.Y && cell.Y <= regionMax.Y
			&& cell.Z >= regionMin.Z && cell.Z <= regionMax.Z);
	}

	private static bool IsClearOfUnits(IReadOnlySet<Coord> cells, AsteroidFieldConfig config)
	{
		foreach (var cell in cells)
		{
			foreach (var unit in config.UnitPositions)
			{
				if (ChebyshevDistance(cell, unit) <= config.UnitClearance)
					return false;
			}
		}

		return true;
	}

	private static bool IsClearOfAsteroids(
		IReadOnlySet<Coord> cells,
		IReadOnlyList<BattleHazardSpawn> placed,
		int gap)
	{
		foreach (var asteroid in placed)
		{
			foreach (var cell in cells)
			{
				if (asteroid.Cells.Any(other => ChebyshevDistance(cell, other) <= gap))
					return false;
			}
		}

		return true;
	}

	private static int ChebyshevDistance(Coord a, Coord b) =>
		System.Math.Max(
			System.Math.Max(System.Math.Abs(a.X - b.X), System.Math.Abs(a.Y - b.Y)),
			System.Math.Abs(a.Z - b.Z));
}
