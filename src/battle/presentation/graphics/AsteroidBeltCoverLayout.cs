using GrimSpace.Battle.NonUnits;

namespace GrimSpace.Battle.Presentation.Graphics;

internal readonly record struct AsteroidBeltRockPlacement(
	float X,
	float Y,
	float Z,
	float Diameter);

internal static class AsteroidBeltCoverLayout
{
	private const int LowRockCount = 24;
	private const int HighRockCount = 48;

	public static IReadOnlyList<AsteroidBeltRockPlacement> Create(
		int resistancePercent,
		int seed)
	{
		if (resistancePercent is not (
			AsteroidBelt.LowResistancePercent or AsteroidBelt.HighResistancePercent))
			throw new ArgumentOutOfRangeException(nameof(resistancePercent));

		var random = new Random(seed);
		var count = resistancePercent == AsteroidBelt.LowResistancePercent
			? LowRockCount
			: HighRockCount;
		var placements = new AsteroidBeltRockPlacement[count];
		for (var i = 0; i < count; i++)
		{
			var diameter = 0.06f + (float)random.NextDouble() * 0.05f;
			const float halfRange = 0.52f;
			placements[i] = new AsteroidBeltRockPlacement(
				((float)random.NextDouble() * 2f - 1f) * halfRange,
				((float)random.NextDouble() * 2f - 1f) * halfRange,
				((float)random.NextDouble() * 2f - 1f) * halfRange,
				diameter);
		}

		return placements;
	}
}
