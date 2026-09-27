using GrimSpace.Math.Grid;
using GrimSpace.Units.Loadouts.Abilities;
using Microsoft.Extensions.Caching.Memory;

namespace GrimSpace.Battle.Spatial;

public static class AbilityArea
{
	private const int CacheSizeLimit = 256;

	private static readonly MemoryCache Cache =
		new(new MemoryCacheOptions { SizeLimit = CacheSizeLimit });

	public static HashSet<Coord> CellsInBounds(
		IAreaDamage area,
		BodyFrame frame,
		ESpatialOrientation direction,
		Grid grid)
	{
		if (area is not AbilitySpec spec)
			return Compute(area, frame, direction, grid);

		var key = (
			frame.Origin,
			frame.Fore,
			frame.Dorsal,
			frame.Starboard,
			direction,
			grid.Width,
			grid.Height,
			grid.Depth,
			spec);

		return Cache.GetOrCreate(key, entry =>
		{
			entry.Size = 1;
			return Compute(area, frame, direction, grid);
		})!;
	}

	private static HashSet<Coord> Compute(
		IAreaDamage area,
		BodyFrame frame,
		ESpatialOrientation direction,
		Grid grid)
	{
		var burstDirection = frame.Step(direction);
		var result = new HashSet<Coord>();
		foreach (var cell in area.GetArea(frame.Origin, burstDirection, frame.Fore, frame.Dorsal))
		{
			if (grid.IsInBounds(cell))
				result.Add(cell);
		}

		return result;
	}
}
