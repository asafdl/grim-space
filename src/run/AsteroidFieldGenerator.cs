using GrimSpace.Battle.Board;
using GrimSpace.Math.Grid;

namespace GrimSpace.Run;

public static class AsteroidFieldGenerator
{
	private static readonly string[] LargeVisuals = ["rock_large_a", "rock_large_b", "rock"];
	private static readonly string[] MediumVisuals = ["rock", "rock_large_a", "rock_large_b"];

	public static IReadOnlyList<BoardHazardSpawn> Generate(AsteroidFieldConfig config)
	{
		var rng = new Random(config.Seed);
		var placed = new List<BoardHazardSpawn>();
		var maxAttempts = config.TargetCount * 40;

		for (var attempt = 0; attempt < maxAttempts && placed.Count < config.TargetCount; attempt++)
		{
			var radius = PickRadius(rng);
			if (!TryPickCenter(config, rng, radius, out var center))
				continue;

			if (!IsClearOfUnits(center, radius, config))
				continue;

			if (!IsClearOfAsteroids(center, radius, placed, config.AsteroidGap))
				continue;

			placed.Add(new BoardHazardSpawn
			{
				Center = center,
				Radius = radius,
				VisualId = PickVisual(rng, radius),
			});
		}

		return placed;
	}

	private static int PickRadius(Random rng) =>
		rng.Next(100) switch
		{
			< 50 => 2,
			< 90 => 1,
			_ => 1,
		};

	private static bool TryPickCenter(AsteroidFieldConfig config, Random rng, int radius, out Coord center)
	{
		var blockRadius = Hazard.BlockRadiusFor(radius);
		var min = blockRadius + config.RegionMargin;
		var max = config.GridSize - blockRadius - 1 - config.RegionMargin;
		if (min > max)
		{
			center = default;
			return false;
		}

		var offset = new Coord(
			rng.Next(-config.RegionHalfExtent, config.RegionHalfExtent + 1),
			rng.Next(-config.RegionHalfExtent, config.RegionHalfExtent + 1),
			rng.Next(-config.RegionHalfExtent, config.RegionHalfExtent + 1));
		center = config.RegionCenter + offset;

		return center.X >= min && center.X <= max
			&& center.Y >= min && center.Y <= max
			&& center.Z >= min && center.Z <= max;
	}

	private static bool IsClearOfUnits(Coord center, int radius, AsteroidFieldConfig config)
	{
		var blockRadius = Hazard.BlockRadiusFor(radius);
		foreach (var unit in config.UnitPositions)
		{
			if (ChebyshevDistance(center, unit) <= blockRadius + config.UnitClearance)
				return false;
		}

		return true;
	}

	private static bool IsClearOfAsteroids(
		Coord center,
		int radius,
		IReadOnlyList<BoardHazardSpawn> placed,
		int gap)
	{
		var blockRadius = Hazard.BlockRadiusFor(radius);
		foreach (var asteroid in placed)
		{
			if (ChebyshevDistance(center, asteroid.Center) <= blockRadius + Hazard.BlockRadiusFor(asteroid.Radius) + gap)
				return false;
		}

		return true;
	}

	private static string PickVisual(Random rng, int radius) =>
		radius >= 2
			? LargeVisuals[rng.Next(LargeVisuals.Length)]
			: MediumVisuals[rng.Next(MediumVisuals.Length)];

	private static int ChebyshevDistance(Coord a, Coord b) =>
		System.Math.Max(
			System.Math.Max(System.Math.Abs(a.X - b.X), System.Math.Abs(a.Y - b.Y)),
			System.Math.Abs(a.Z - b.Z));
}
