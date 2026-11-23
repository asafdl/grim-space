using System.Numerics;
using GrimSpace.Math.Grid;

namespace GrimSpace.Battle.Presentation.Graphics;

internal readonly record struct CloudRegion(Vector3 Center, Vector3 Radius, Vector3 Offset, Vector3 Rotation);

internal static class CloudPattern
{
	public static int SeedFor(int encounterSeed, Coord cell)
	{
		unchecked
		{
			var hash = 2166136261u;
			hash = (hash ^ (uint)encounterSeed) * 16777619u;
			hash = (hash ^ (uint)cell.X) * 16777619u;
			hash = (hash ^ (uint)cell.Y) * 16777619u;
			hash = (hash ^ (uint)cell.Z) * 16777619u;
			return (int)(hash & int.MaxValue);
		}
	}

	public static CloudRegion[] Create(int seed)
	{
		ArgumentOutOfRangeException.ThrowIfNegative(seed);
		var random = new Random(seed);
		var regions = new CloudRegion[random.Next(2, 5)];
		float Between(float min, float max) => min + (max - min) * random.NextSingle();

		for (var i = 0; i < regions.Length; i++)
		{
			var x = -0.24f + 0.48f * i / (regions.Length - 1);
			regions[i] = new CloudRegion(
				new Vector3(x + Between(-0.06f, 0.06f), Between(-0.13f, 0.13f), Between(-0.1f, 0.1f)),
				new Vector3(Between(0.20f, 0.28f), Between(0.17f, 0.24f), Between(0.22f, 0.30f)),
				new Vector3(Between(-0.035f, 0.035f), Between(-0.035f, 0.035f), Between(-0.035f, 0.035f)),
				new Vector3(Between(-0.3f, 0.3f), Between(-0.5f, 0.5f), Between(-0.3f, 0.3f)));
		}

		return regions;
	}

	public static int FindRegion(Vector3 position, IReadOnlyList<CloudRegion> regions)
	{
		ArgumentNullException.ThrowIfNull(regions);
		var selected = -1;
		var bestDistance = 1f;
		for (var i = 0; i < regions.Count; i++)
		{
			var relative = (position - regions[i].Center) / regions[i].Radius;
			var distance = relative.LengthSquared();
			if (distance < bestDistance)
			{
				bestDistance = distance;
				selected = i;
			}
		}

		return selected;
	}
}
