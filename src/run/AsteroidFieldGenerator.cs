using GrimSpace.Math.Grid;

namespace GrimSpace.Run;

public static class AsteroidFieldGenerator
{
	private static readonly Coord[] Neighbors =
	[
		new(1, 0, 0), new(-1, 0, 0),
		new(0, 1, 0), new(0, -1, 0),
		new(0, 0, 1), new(0, 0, -1),
	];

	public static IReadOnlyList<WorldHazardSpawn> Generate(AsteroidFieldConfig config)
	{
		var rng = new Random(config.Seed);
		var placed = new List<WorldHazardSpawn>();
		var maxAttempts = config.TargetCount * 100;

		for (var attempt = 0; attempt < maxAttempts && placed.Count < config.TargetCount; attempt++)
		{
			var localCells = GrowShape(rng, PickCellCount(rng));
			var origin = PickOrigin(config, rng);
			var cells = localCells.Select(cell => cell + origin).ToHashSet();

			if (!FitsField(cells, config))
				continue;

			if (!IsClearOfUnits(cells, config))
				continue;

			if (!IsClearOfAsteroids(cells, placed, config.AsteroidGap))
				continue;

			placed.Add(new WorldHazardSpawn { Origin = origin, Cells = cells });
		}

		return placed;
	}

	private static int PickCellCount(Random rng) =>
		rng.Next(100) switch
		{
			< 35 => rng.Next(1, 4),
			< 85 => rng.Next(4, 10),
			_ => rng.Next(10, 19),
		};

	private static HashSet<Coord> GrowShape(Random rng, int targetCount)
	{
		var cells = new HashSet<Coord> { Coord.Zero };
		var insertionOrder = new List<Coord> { Coord.Zero };
		var axisWeights = PickAxisWeights(rng);

		while (cells.Count < targetCount)
		{
			var source = insertionOrder[rng.Next(insertionOrder.Count)];
			var direction = PickDirection(rng, axisWeights);
			var candidate = source + direction;
			if (cells.Add(candidate))
				insertionOrder.Add(candidate);
		}

		return cells;
	}

	private static (int X, int Y, int Z) PickAxisWeights(Random rng)
	{
		var mode = rng.Next(3);
		var dominantAxis = rng.Next(3);
		return mode switch
		{
			0 => (3, 3, 3),
			1 => AxisWeights(dominantAxis, dominant: 8, secondary: 1),
			_ => AxisWeights(dominantAxis, dominant: 1, secondary: 5),
		};
	}

	private static (int X, int Y, int Z) AxisWeights(int axis, int dominant, int secondary) =>
		axis switch
		{
			0 => (dominant, secondary, secondary),
			1 => (secondary, dominant, secondary),
			_ => (secondary, secondary, dominant),
		};

	private static Coord PickDirection(Random rng, (int X, int Y, int Z) weights)
	{
		var total = weights.X + weights.Y + weights.Z;
		var roll = rng.Next(total);
		var axis = roll < weights.X ? 0 : roll < weights.X + weights.Y ? 1 : 2;
		return Neighbors[axis * 2 + rng.Next(2)];
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
		IReadOnlyList<WorldHazardSpawn> placed,
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
