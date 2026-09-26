using GrimSpace.Battle.Encounter;
using GrimSpace.Math.Grid;

namespace GrimSpace.Battle.Encounter.Generation;

public static class AsteroidFieldGenerator
{
	public static IReadOnlyList<BattleHazardSpawn> Generate(AsteroidFieldConfig config)
	{
		var rng = new Random(config.Seed);
		var placed = new List<BattleHazardSpawn>();
		var bounds = new List<(Coord Min, Coord Max)>();
		var maxAttempts = config.TargetCount * 100;

		var largeCount = System.Math.Min(config.TargetCount, 3 + rng.Next(3));
		for (var i = 0; i < largeCount && placed.Count == i; i++)
		{
			var size = new Coord(10 + rng.Next(4), 12 + rng.Next(4), 5 + rng.Next(3));
			size = rng.Next(3) switch
			{
				0 => size,
				1 => new Coord(size.Y, size.Z, size.X),
				_ => new Coord(size.Z, size.X, size.Y),
			};
			for (var attempt = 0; attempt < 100 && placed.Count == i; attempt++)
				TryPlace(size);
		}

		for (var attempt = 0; attempt < maxAttempts && placed.Count < config.TargetCount; attempt++)
		{
			var size = PickSize(rng);
			TryPlace(new Coord(size, size, size));
		}

		return placed;

		void TryPlace(Coord size)
		{
			var localCells = CreateVolume(size);
			var origin = PickOrigin(config, rng);
			var cells = localCells.Select(cell => cell + origin).ToHashSet();
			var min = origin - new Coord(size.X / 2, size.Y / 2, size.Z / 2);
			var max = min + size - new Coord(1, 1, 1);

			if (!FitsField(cells, config))
				return;

			if (!IsClearOfUnits(cells, config))
				return;

			if (!IsClearOfAsteroids(min, max, bounds, config.AsteroidGap))
				return;

			placed.Add(new BattleHazardSpawn { Origin = origin, Cells = cells });
			bounds.Add((min, max));
		}
	}

	private static int PickSize(Random rng) =>
		rng.Next(100) switch
		{
			< 20 => 1,
			< 60 => 2,
			< 92 => 3,
			_ => 4,
		};

	private static HashSet<Coord> CreateVolume(Coord size)
	{
		var cells = new HashSet<Coord>();
		var minX = -(size.X / 2);
		var minY = -(size.Y / 2);
		var minZ = -(size.Z / 2);
		for (var x = minX; x < minX + size.X; x++)
		for (var y = minY; y < minY + size.Y; y++)
		for (var z = minZ; z < minZ + size.Z; z++)
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
		Coord min,
		Coord max,
		IReadOnlyList<(Coord Min, Coord Max)> placed,
		int gap)
	{
		foreach (var (otherMin, otherMax) in placed)
		{
			if (min.X <= otherMax.X + gap && max.X >= otherMin.X - gap
				&& min.Y <= otherMax.Y + gap && max.Y >= otherMin.Y - gap
				&& min.Z <= otherMax.Z + gap && max.Z >= otherMin.Z - gap)
				return false;
		}

		return true;
	}

	private static int ChebyshevDistance(Coord a, Coord b) =>
		System.Math.Max(
			System.Math.Max(System.Math.Abs(a.X - b.X), System.Math.Abs(a.Y - b.Y)),
			System.Math.Abs(a.Z - b.Z));
}
